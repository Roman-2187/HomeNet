using Dapper;
using System.Data;

namespace HomeNetOrm.Helpers
{
    public class GuidTypeHandler : SqlMapper.TypeHandler<Guid>
    {
        public override void SetValue(IDbDataParameter parameter, Guid value)
        {
            parameter.Value = value.ToString();
        }

        public override Guid Parse(object value)
        {
            return value switch
            {
                Guid guid => guid,
                string str when Guid.TryParse(str, out Guid parsedGuid) => parsedGuid,
                _ => Guid.Empty
            };
        }
    }
}
