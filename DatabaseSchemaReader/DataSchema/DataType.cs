﻿using System;
using System.Diagnostics;

namespace DatabaseSchemaReader.DataSchema
{
    /// <summary>
    /// Maps between database datatypes and .Net datatypes.
    /// </summary>
    [Serializable]
    public class DataType
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool? _isString;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool? _isFloat;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool? _isInt;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool? _isNumeric;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool? _isDateTime;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _netDataTypeCsName;

        public DataType(string typeName, string netDataType)
        {
            TypeName = typeName;
            NetDataType = netDataType;
        }
        ///<summary>
        ///The provider-specific data type name.
        ///</summary>
        public string TypeName { get; private set; }

        ///<summary>
        ///The name of the .NET Framework type of the data type.
        ///</summary>
        public string NetDataType { get; private set; }

        /// <summary>
        /// Gets the name of the C# net data type.
        /// </summary>
        public string NetDataTypeCsName
        {
            get
            {
                if (!string.IsNullOrEmpty(_netDataTypeCsName))
                    return _netDataTypeCsName;
                //can we use NetDataType
                if (string.IsNullOrEmpty(NetDataType))
                    return null;
                if (IsString)
                    _netDataTypeCsName = "string";
                else if (IsInt)
                    _netDataTypeCsName = "int";
                else if (IsFloat)
                    _netDataTypeCsName = "float";
                else if (IsDateTime)
                    _netDataTypeCsName = "DateTime";
                else
                {
                    //check others
                    Type t = GetNetType();
                    if (t == typeof(bool))
                        _netDataTypeCsName = "bool";
                    else if (t == typeof(short))
                        _netDataTypeCsName = "short";
                    else if (t == typeof(long))
                        _netDataTypeCsName = "long";
                    else if (t == typeof(decimal))
                        _netDataTypeCsName = "decimal";
                    else if (t == typeof(double))
                        _netDataTypeCsName = "double";
                    else
                        _netDataTypeCsName = NetDataType;
                }

                return _netDataTypeCsName;
            }
            set
            {
                _netDataTypeCsName = value;
            }
        }

        /// <summary>
        /// Gets the name of the C# net data type, correcting for the column properties
        /// </summary>
        /// <param name="column">The column.</param>
        /// <returns></returns>
        public string NetCodeName(DatabaseColumn column)
        {
            if (!IsNumeric || IsInt) return NetDataTypeCsName;
            var precision = column.Precision.GetValueOrDefault();
            var scale = column.Scale.GetValueOrDefault();
            return NetNameForIntegers(scale, precision);
        }

        /// <summary>
        /// Gets the name of the C# net data type, correcting for the argument properties
        /// </summary>
        /// <param name="argument">The argument.</param>
        /// <returns></returns>
        public string NetCodeName(DatabaseArgument argument)
        {
            if (!IsNumeric || IsInt) return NetDataTypeCsName;
            var precision = argument.Precision.GetValueOrDefault();
            var scale = argument.Scale.GetValueOrDefault();
            return NetNameForIntegers(scale, precision);
        }

        private string NetNameForIntegers(int scale, int precision)
        {
            if (scale != 0 || precision >= 19) return NetDataTypeCsName;

            //could be a short, int or long...
            //VARCHAR2(10) is common for Oracle integers, but it can overflow an int
            //int.MaxValue is 2147483647 so +1 is allowable in the database
            if (precision > 10) //up to long.MaxValue
            {
                return "long";
            }
            if (precision > 4) //2147483647
            {
                return "int";
            }
            if (precision > 1)
            {
                return "short";
            }
            return NetDataTypeCsName;
        }

        /// <summary>
        /// Gets the type of the net data type.
        /// </summary>
        public Type GetNetType()
        {
            if (string.IsNullOrEmpty(NetDataType))
                return null;
            Type t = Type.GetType(NetDataType);
            return t;
        }

        /// <summary>
        /// Returns if this is a System.String
        /// </summary>
        public bool IsString
        {
            get
            {
                if (_isString.HasValue)
                    return _isString.Value;
                if (string.IsNullOrEmpty(NetDataType))
                    return false;
                _isString = (Type.GetType(NetDataType) == typeof(string));
                return _isString.Value;
            }
        }

        /// <summary>
        /// Returns if this is a large System.String (text, ntext, clob)
        /// </summary>
        public bool IsStringClob
        {
            get
            {
                if (!IsString) return false;
                //(n)text or (n)varchar
                return (TypeName.EndsWith("text", StringComparison.OrdinalIgnoreCase) || string.Equals("CLOB", TypeName, StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is a datetime.
        /// </summary>
        public bool IsDateTime
        {
            get
            {
                if (_isDateTime.HasValue)
                    return _isDateTime.Value;
                if (string.IsNullOrEmpty(NetDataType))
                    return false;
                _isDateTime = (Type.GetType(NetDataType) == typeof(DateTime));
                return _isDateTime.Value;
            }
        }

        /// <summary>
        /// Returns if this is a System.Single (float)
        /// </summary>
        public bool IsFloat
        {
            get
            {
                if (_isFloat.HasValue)
                    return _isFloat.Value;
                if (string.IsNullOrEmpty(NetDataType))
                    return false;
                _isFloat = (Type.GetType(NetDataType) == typeof(float));
                return _isFloat.Value;
            }
        }
        /// <summary>
        /// Returns if this is a System.Int32 (int)
        /// </summary>
        public bool IsInt
        {
            get
            {
                if (_isInt.HasValue)
                    return _isInt.Value;
                if (string.IsNullOrEmpty(NetDataType))
                    return false;
                _isInt = (Type.GetType(NetDataType) == typeof(int));
                return _isInt.Value;
            }
        }

        /// <summary>
        /// Returns if this is a numeric value (int, float, decimal)
        /// </summary>
        public bool IsNumeric
        {
            get
            {
                if (_isNumeric.HasValue)
                    return _isNumeric.Value;
                if (string.IsNullOrEmpty(NetDataType))
                    return false;
                Type type = Type.GetType(NetDataType);
                switch (Type.GetTypeCode(type))
                {
                    //Char may be a decimal...
                    case TypeCode.Decimal:
                    case TypeCode.SByte:
                    case TypeCode.Byte:
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                    case TypeCode.Single:
                    case TypeCode.Double:
                        _isNumeric = true;
                        break;
                    default:
                        _isNumeric = false;
                        break;
                }
                return _isNumeric.Value;
            }
        }

        ///<summary>
        ///The provider-specific type value that should be used when specifying a parameter’s type. For example, SqlDbType.Money or OracleType.Blob.
        ///</summary>
        public int ProviderDbType { get; set; }

        ///<summary>
        ///Format string that represents how to add this column to a data definition statement, such as CREATE TABLE. For example, the SQL data type DECIMAL needs a precision and a scale. In this case, the format string would be “DECIMAL({0},{1})”.
        ///</summary>
        public string CreateFormat { get; set; }

        ///<summary>
        ///LiteralPrefix. Eg in Oracle dates, TO_DATE('
        ///</summary>
        public string LiteralPrefix { get; set; }

        ///<summary>
        ///LiteralSuffix. Eg in Oracle dates, ','YYYY-MM-DD HH24:MI:SS')
        ///</summary>
        public string LiteralSuffix { get; set; }

        public override string ToString()
        {
            return TypeName + " = " + NetDataType;
        }
    }
}