using System;
using System.Collections.Generic;
using System.Linq;
using Common.Constants;
using Common.Enums;

namespace Common.Adapters
{
    public class ExpertParametersAdapter : IExpertParametersAdapter
    {
        public T GetParameters<T>(IReadOnlyDictionary<string, object> parameters)
            where T : new()
        {
            var parsedParams = new T();
            var fieldValues = parsedParams.GetType().GetProperties();

            foreach (var val in fieldValues)
            {
                var keyName = parameters.Keys.FirstOrDefault(x => x.ToLower() == val.Name.ToLower());
                if (keyName == null)
                {
                    continue;
                }

                parameters.TryGetValue(keyName, out object tmpValue);

                System.Reflection.PropertyInfo myPropInfo;
                if (val.PropertyType.IsEnum)
                {
                    tmpValue = (string)tmpValue == PlatformParameters.Buy ? OrderBuySell.Buy : OrderBuySell.Sell;
                    myPropInfo = parsedParams.GetType().GetProperty(val.Name);
                    myPropInfo.SetValue(parsedParams, tmpValue, null);
                    continue;
                }

                var typeCode = Type.GetTypeCode(val.PropertyType);
                switch (typeCode)
                {
                    case TypeCode.Int32:
                        myPropInfo = parsedParams.GetType().GetProperty(val.Name);
                        myPropInfo.SetValue(parsedParams, Convert.ToInt32((string)tmpValue), null);
                        break;
                    case TypeCode.Decimal:
                        myPropInfo = parsedParams.GetType().GetProperty(val.Name);
                        myPropInfo.SetValue(parsedParams, Convert.ToDecimal((string)tmpValue), null);
                        break;
                    case TypeCode.Boolean:
                        myPropInfo = parsedParams.GetType().GetProperty(val.Name);
                        myPropInfo.SetValue(parsedParams, Convert.ToBoolean((string)tmpValue), null);
                        break;
                    case TypeCode.String:
                        myPropInfo = parsedParams.GetType().GetProperty(val.Name);
                        myPropInfo.SetValue(parsedParams, (string)tmpValue, null);
                        break;
                }
            }

            return parsedParams;
        }
    }
}
