using System;
using TwinCAT.Ads;
using TwinCAT.Ads.TypeSystem;
using TwinCAT.TypeSystem;

namespace Mbc.Ads.Helper.ReadType
{
    class Program
    {
        static void Main(string[] args)
        {
            using var client = new AdsClient();
            client.Connect(args[0], 851);

            IAdsSymbol symbol = client.ReadSymbol(args[1]);

            DumpSymbol(symbol);
        }

        private static void DumpSymbol(IAdsSymbol symbol)
        {
            if (symbol is IStructInstance structInst)
            {
                Console.WriteLine($"Type=IStructType, ByteSize={structInst.ByteSize}");

                foreach (IMember subItem in ((StructType)structInst.DataType).Members)
                {
                    DumpMember(subItem, "  ");
                }
            }
        }

        private static void DumpMember(IMember member, string prefix)
        {
            Console.WriteLine($"{prefix}Offset={member.Offset} InstanceName={member.InstanceName} DataType.Category={member.DataType.Category}");

            switch (member.DataType.Category)
            {
                case DataTypeCategory.Primitive:
                    DumpPrimitive((IPrimitiveType)member.DataType, prefix + "  ");
                    break;
                case DataTypeCategory.Enum:
                    DumpEnum((IEnumType)member.DataType, prefix + "  ");
                    break;
                case DataTypeCategory.Array:
                    DumpArray((IArrayType)member.DataType, prefix + "  ");
                    break;
                case DataTypeCategory.String:
                    DumpString((IStringType)member.DataType, prefix + "  ");
                    break;
                default:
                    Console.WriteLine($"{prefix}  Not dumped");
                    break;
            }
        }

        private static void DumpString(IStringType stringType, string prefix)
        {
            Console.WriteLine($"{prefix}ManagedType={GetManagedType(stringType)} Length={stringType.Length} ByteSize={stringType.ByteSize} Encoding={stringType.Encoding}");
        }

        private static void DumpArray(IArrayType arrayType, string prefix)
        {
            Console.WriteLine($"{prefix}ElementType.Category={arrayType.ElementType.Category} ElementType.ManagedType={GetManagedType(arrayType.ElementType)} Dimensions.ElementCount={arrayType.Dimensions.ElementCount}");
        }

        private static void DumpEnum(IEnumType enumType, string prefix)
        {
            Console.WriteLine($"{prefix} BaseType.ManagedType={GetManagedType(enumType.BaseType)}");
        }

        private static void DumpPrimitive(IPrimitiveType primitiveType, string prefix)
        {
            Console.WriteLine($"{prefix} ManagedType={GetManagedType(primitiveType)}");
        }

        private static Type GetManagedType(IDataType dataType)
        {
            return ((IManagedMappableType)dataType).ManagedType;
        }
    }
}
