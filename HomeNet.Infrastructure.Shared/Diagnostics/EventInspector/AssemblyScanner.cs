using System.Reflection;
using HomeNet.Infrastructure.Shared.Enums;

namespace HomeNet.Infrastructure.Shared.Diagnostics.EventInspector
{
    public static class AssemblyScanner
    {
        private const byte OpCodeCall = 0x28;
        private const byte OpCodeCallVirt = 0x6F;

        public static void ScanPublishers(
            Dictionary<string, ComponentNode> nodes,
            Dictionary<Type, MessageState> registry,
            Func<string, ComponentNode> getOrCreateNode)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.FullName != null && a.FullName.StartsWith("HomeNet"));

            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (!type.IsClass || type.IsAbstract) continue;

                    var componentName = type.Name.StartsWith("<") && type.DeclaringType != null
                        ? type.DeclaringType.Name
                        : type.Name;

                    var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
                    var methods = type.GetMethods(flags).Cast<MethodBase>().Concat(type.GetConstructors(flags));

                    foreach (var method in methods)
                    {
                        try
                        {
                            // 🔥 ПРОПУСКАЕМ методы из системных сборок и Avalonia (они не наши)
                            var declaringTypeName = method.DeclaringType?.FullName ?? "";
                            if (declaringTypeName.StartsWith("System") || declaringTypeName.StartsWith("Avalonia"))
                                continue;

                            var body = method.GetMethodBody();
                            var il = body?.GetILAsByteArray();
                            if (il == null || il.Length == 0) continue;

                            // Теперь метод статический, вызов сработает корректно
                            ParseMethodIL(il, method, componentName, getOrCreateNode, registry);
                        }
                        catch { }
                    }
                }
            }
        }

        // 🔥 Сделали метод static, исправили byte[] il и genericArgs[0]
        private static void ParseMethodIL(
            byte[] il,
            MethodBase method,
            string componentName,
            Func<string, ComponentNode> getOrCreateNode,
            Dictionary<Type, MessageState> registry)
        {
            var i = 0;
            while (i < il.Length)
            {
                var opCode = il[i++];
                if (opCode != OpCodeCall && opCode != OpCodeCallVirt) continue;
                if (i + 4 > il.Length) break;

                var token = BitConverter.ToInt32(il, i);
                i += 4;

                try
                {
                    // 🔥 Ошибочная проверка token >= il.Length удалена.
                    // Токен метаданных берется из модуля, а не из массива IL-байтов метода.
                    var called = method.Module.ResolveMethod(token);

                    if (called != null && called is { IsGenericMethod: true, Name: "Publish" or "PublishAsync" or "PublishFact" })
                    {
                        var genericArgs = called.GetGenericArguments();
                        if (genericArgs == null || genericArgs.Length == 0) continue;

                        // Достаем первый generic-параметр (тип события)
                        var eventType = genericArgs[0];

                        // 🔥 Игнорируем системные типы
                        if (eventType.FullName != null && eventType.FullName.StartsWith("System."))
                            continue;

                        var node = getOrCreateNode(componentName);

                        if (!node.PublishedMessages.Contains(eventType))
                            node.PublishedMessages.Add(eventType);

                        if (!registry.ContainsKey(eventType))
                            registry[eventType] = MessageState.None;

                        registry[eventType] |= MessageState.Published;
                    }
                }
                catch
                {
                    // 🔥 Тихий игнор: если токен принадлежит другой сборке, 
                    // ResolveMethod выбросит исключение, и мы просто идем дальше
                }
            }
        }
    }
}
