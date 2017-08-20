using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using loguco.Serialization.Binary.Handlers;

namespace loguco.Serialization.Binary
{
    public static class TypeHandlerCreator
    {
        public static Dictionary<Type, Type> GenericTypeHandlerMakerTypes { get; } =
            new Dictionary<Type, Type>();

        private static readonly Dictionary<Type, GenericTypeHandlerMaker> CreatedMakers =
            new Dictionary<Type, GenericTypeHandlerMaker>();

        /// <summary>
        /// Types that can be directly handled by BinaryReader and BinaryWriter, except String
        /// </summary>
        public static readonly Type[] BasicTypes =
        {
            typeof(bool), typeof(byte), typeof(sbyte), typeof(short), typeof(ushort),
            typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(decimal),
            typeof(float), typeof(double), typeof(char)
        };

        /// <summary>
        /// BinaryWriter's Write overloads for basic types and String
        /// </summary>
        public static readonly ConcurrentDictionary<Type, MethodInfo> BasicTypeWriteMethods =
            new ConcurrentDictionary<Type, MethodInfo>();

        /// <summary>
        /// BinaryReader's ReadType methods for basic types and String
        /// </summary>
        public static readonly ConcurrentDictionary<Type, MethodInfo> BasicTypeReadMethods =
            new ConcurrentDictionary<Type, MethodInfo>();

        /// <summary>
        /// ReferenceTypeHelper.Read method
        /// </summary>
        public static readonly MethodInfo ReferenceTypeHelperRead = typeof(ReferenceTypeHelper).GetMethod("Read");

        /// <summary>
        /// ReferenceTypeHelper.Write method
        /// </summary>
        public static readonly MethodInfo ReferenceTypeHelperWrite = typeof(ReferenceTypeHelper).GetMethod("Write");

        static TypeHandlerCreator()
        {
            foreach (var type in BasicTypes)
            {
                BasicTypeWriteMethods[type] = typeof(BinaryWriter).GetMethod("Write", new [] { type });
                BasicTypeReadMethods[type] = typeof(BinaryReader).GetMethod($"Read{type.Name}");
            }
            // String is not in basic types, so add it here
            BasicTypeWriteMethods[typeof(string)] = typeof(BinaryWriter).GetMethod("Write", new[] { typeof(string) });
            BasicTypeReadMethods[typeof(string)] = typeof(BinaryReader).GetMethod("ReadString");
        }

        /// <summary>
        /// A class that wraps all things needed to create a type's handler
        /// </summary>
        public class HandlerExpressionPackage
        {
            public readonly ParameterExpression _writeBinaryWriter = Expression.Parameter(typeof(BinaryWriter));
            public readonly ParameterExpression _writeObject;
            public readonly ParameterExpression _writeContext = Expression.Parameter(typeof(WriteContext));
            public readonly List<ParameterExpression> _writeLocals = new List<ParameterExpression>();
            public readonly List<Expression> _writeExpressions = new List<Expression>();

            public readonly ParameterExpression _readBinaryReader = Expression.Parameter(typeof(BinaryReader));
            public readonly ParameterExpression _readContext = Expression.Parameter(typeof(ReadContext));
            public readonly ParameterExpression _readObject;
            public readonly List<ParameterExpression> _readLocals = new List<ParameterExpression>();
            public readonly List<Expression> _readExpressions = new List<Expression>();

            public readonly List<Type> _pendingTypes = new List<Type>();

            public HandlerExpressionPackage(Type type)
            {
                _writeObject = Expression.Parameter(type);
                _readObject = Expression.Variable(type);
                _readLocals.Add(_readObject);
            }
        }

        /// <summary>
        /// Create and assign TypeHandler&lt;T&gt;.Writer and TypeHandler&lt;T&gt;.Reader
        /// </summary>
        public static void CreateHandlerForType<T>()
        {
            if (TypeHandler<T>.Writer != null)
            {
                return;
            }

            var type = typeof(T);
            var package = new HandlerExpressionPackage(type);

            if (TryAddExpressionForType(type, package, package._writeObject, out var readValue))
            {
                package._readExpressions.Add(readValue);
            }
            else
            {
                ThrowOnValueType(type);

                // in Write, add:
                // :: ObjectOffsets[_writeObject] = _writeBw.BaseStream.Position
                package._writeExpressions.Add(
                    Expression.Call(
                        Expression.Call(
                            package._writeContext, 
                            typeof(WriteContext).GetProperty("ObjectOffsets").GetMethod),
                    typeof(Dictionary<object, int>).GetMethod("set_Item"),
                    package._writeObject,
                    Expression.Convert(
                        Expression.Call(
                            Expression.Call(
                                package._writeBinaryWriter,
                                typeof(BinaryWriter).GetProperty("BaseStream").GetMethod),
                            typeof(Stream).GetProperty("Position").GetMethod), 
                        typeof(int))
                ));

                // :: _readObject = new T()
                // :: OffsetObjects[_readObject] = _readBr.BaseStream.Position
                package._readExpressions.Add(
                    Expression.Assign(
                        package._readObject,
                        Expression.New(type)
                    ));
                package._readExpressions.Add(
                    Expression.Call(
                        Expression.Call(
                            package._readContext,
                            typeof(ReadContext).GetProperty("OffsetObjects").GetMethod),
                        typeof(Dictionary<int, object>).GetMethod("set_Item"),
                        Expression.Convert(
                            Expression.Call(
                                Expression.Call(
                                    package._readBinaryReader,
                                    typeof(BinaryReader).GetProperty("BaseStream").GetMethod),
                                typeof(Stream).GetProperty("Position").GetMethod),
                            typeof(int)),
                        package._readObject
                    ));

                // check if the type is generic and has a generic type handler maker
                // (this is for List<> and Dictionary<,>)
                var typeDef = type.IsGenericType ? type.GetGenericTypeDefinition() : null;
                if (typeDef != null && GenericTypeHandlerMakerTypes.ContainsKey(typeDef))
                {
                    if (CreatedMakers.TryGetValue(type, out var maker))
                    {
                        maker.AddExpressions(package);
                    }
                    else
                    {
                        var makerType = GenericTypeHandlerMakerTypes[typeDef];
                        if (makerType.IsAbstract || 
                            !typeof(GenericTypeHandlerMaker).IsAssignableFrom(makerType) ||
                            !makerType.IsGenericTypeDefinition)
                            throw new Exception($"Invalid generic type handler maker type {makerType.FullName}");
                        maker = Activator.CreateInstance(makerType.MakeGenericType(type.GetGenericArguments())) as GenericTypeHandlerMaker;
                        CreatedMakers[type] = maker;
                        maker.AddExpressions(package);
                    }
                }
                else
                {
                    AddExpressionsForClassType(type, package);
                }

                // :: return _readObject
                package._readExpressions.Add(package._readObject);
            }

            // add the handler methods
            var writeBlock = Expression.Block(package._writeLocals, package._writeExpressions);
            var writeAction = Expression.Lambda<Action<BinaryWriter, T, WriteContext>>(writeBlock,
                package._writeBinaryWriter, package._writeObject, package._writeContext).Compile();
            TypeHandler<T>.Writer = writeAction;

            var readBlock = Expression.Block(package._readLocals, package._readExpressions);
            var readFunc = Expression.Lambda<Func<BinaryReader, ReadContext, T>>(readBlock, package._readBinaryReader,
                package._readContext).Compile();
            TypeHandler<T>.Reader = readFunc;

            // process types found in the progress
            foreach (var pendingType in package._pendingTypes)
            {
                typeof(BinarySerializer).GetMethod("Initialize").MakeGenericMethod(pendingType).Invoke(null, null);
            }
        }

        /// <summary>
        /// Add expressions in Write and Read methods of a general class type's handler
        /// </summary>
        private static void AddExpressionsForClassType(Type type, HandlerExpressionPackage package)
        {
            // for a general class object, write its properties one by one
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();
            properties.Sort((p1, p2) => String.CompareOrdinal(p1.Name, p2.Name));
            foreach (var prop in properties)
            {
                if (!prop.CanRead || prop.GetCustomAttribute<NonSerializedAttribute>() != null)
                    continue;

                var propType = prop.PropertyType;
                var isList = false;
                var isDict = false;

                // skip read-only properties
                // unless the property is List<> or Dictionary<,>
                // (this is to handle readonly lists/dictionaries, such as: public List<int> Values { get; } => new List<int>();)
                if (!prop.CanWrite)
                {
                    if (!propType.IsGenericType)
                        continue;
                    var genericDef = propType.GetGenericTypeDefinition();
                    isList = genericDef == typeof(List<>);
                    isDict = genericDef == typeof(Dictionary<,>);
                    if (!isList && !isDict)
                        continue;
                }

                // see if the property is basic type
                if (TryAddExpressionForType(propType, package, Expression.Call(package._writeObject, prop.GetMethod), out var readValueExpr))
                {
                    package._readExpressions.Add(Expression.Call(package._readObject, prop.SetMethod, readValueExpr));
                    continue;
                }

                ThrowOnValueType(propType);

                // :: ReferenceTypeHelper.Write(_writeBw, _writeObject.Property, _writeContext);
                package._writeExpressions.Add(
                    Expression.Call(
                        ReferenceTypeHelperWrite.MakeGenericMethod(propType),
                        package._writeBinaryWriter,
                        Expression.Call(package._writeObject, prop.GetMethod),
                        package._writeContext
                    ));

                if (prop.CanWrite)
                {
                    // :: _readObject.Property = ReferenceTypeHelper.Read(_readBr, _readContext);
                    package._readExpressions.Add(
                        Expression.Call(
                            package._readObject,
                            prop.SetMethod,
                            Expression.Call(
                                ReferenceTypeHelperRead.MakeGenericMethod(propType),
                                package._readBinaryReader,
                                package._readContext
                            )));
                }
                else
                {
                    // for a readonly list/dictionary,
                    // first read, then call Add/AddRange methods
                    // TODO: performance can be improved by eliminating the intermediate list/dictionary

                    // :: readTemp = ReferenceTypeHelper.Read(_readBr, _readContext);
                    var readTemp = Expression.Variable(propType);
                    package._readLocals.Add(readTemp);
                    package._readExpressions.Add(
                        Expression.Assign(readTemp,
                            Expression.Call(
                                ReferenceTypeHelperRead.MakeGenericMethod(propType),
                                package._readBinaryReader,
                                package._readContext
                            )));
                    if (isList)
                    {
                        // :: _readObject.Property.AddRange(readTemp);
                        package._readExpressions.Add(
                            Expression.Call(
                                Expression.Call(package._readObject, prop.GetMethod),
                                propType.GetMethod("AddRange"),
                                readTemp));
                    }
                    else if (isDict)
                    {
                        // :: DictionaryHandlerHelper.CopyDictionary(_readObject.Property, readTemp);
                        package._readExpressions.Add(
                            Expression.Call(
                                typeof(DictionaryHandlerHelper).GetMethod("CopyDictionary").MakeGenericMethod(propType.GetGenericArguments()),
                                Expression.Call(package._readObject, prop.GetMethod),
                                readTemp));
                    }
                }

                // create the handler for the property's type
                package._pendingTypes.Add(propType);
            }
        }

        /// <summary>
        /// Try to create write/read expressions for a type. <para/>
        /// It succeeds if the type is a basic type, a value type with existing handler, a nullable type of the previous two, or string.
        /// </summary>
        /// <param name="type">The type of value to read/write</param>
        /// <param name="valueExpr">The value to write</param>
        /// <param name="binaryWriter">The BinaryWriter object</param>
        /// <param name="binaryReader">The BinaryReader object</param>
        /// <param name="writeContext">The WriteContext object</param>
        /// <param name="readContext">The ReadContext object</param>
        /// <param name="writeExprs">Write expressions</param>
        /// <param name="readExprs">Read expressions</param>
        /// <param name="writeLocals">Locals used in write expressions</param>
        /// <param name="readLocals">Locals used in read expressions</param>
        /// <param name="readValueExpr">The variable containing the read value</param>
        /// <returns>True if succeeds, false otherwise</returns>
        public static bool TryCreateExpressionForType(
            Type type, Expression valueExpr, Expression binaryWriter, Expression binaryReader, Expression writeContext, Expression readContext, 
            out List<Expression> writeExprs, out List<Expression> readExprs, out List<ParameterExpression> writeLocals, out List<ParameterExpression> readLocals,
            out Expression readValueExpr)
        {
            writeExprs = new List<Expression>();
            readExprs = new List<Expression>();
            writeLocals = new List<ParameterExpression>();
            readLocals = new List<ParameterExpression>();

            // case 1. basic type, simply use BinaryWriter/BinaryReader methods
            if (BasicTypes.Contains(type))
            {
                writeExprs.Add(
                    Expression.Call(
                        binaryWriter,
                        BasicTypeWriteMethods[type],
                        valueExpr
                    ));
                readValueExpr = Expression.Call(binaryReader, BasicTypeReadMethods[type]);
                return true;
            }

            if (type.IsValueType)
            {
                // case 2. value type with existing handler, call the handler
                var handler = typeof(TypeHandler<>).MakeGenericType(type);
                var writer = handler.GetProperty("Writer").GetValue(null);
                var reader = handler.GetProperty("Reader").GetValue(null);
                if (writer != null && reader != null)
                {
                    writeExprs.Add(
                        Expression.Call(
                            Expression.Call(handler.GetProperty("Writer").GetMethod),
                            writer.GetType().GetMethod("Invoke"),
                            binaryWriter,
                            valueExpr,
                            writeContext
                        ));
                    readValueExpr = Expression.Call(
                        Expression.Call(handler.GetProperty("Reader").GetMethod),
                        reader.GetType().GetMethod("Invoke"),
                        binaryReader,
                        readContext
                    );

                    return true;
                }

                // case 3. nullable type of the previous two, use custom rule:
                // [true][value] if HasValue
                // [false]       otherwise (i.e. null)
                var underlyingType = Nullable.GetUnderlyingType(type);
                if (underlyingType != null)
                {
                    handler = typeof(TypeHandler<>).MakeGenericType(underlyingType);

                    var isBasicType = BasicTypes.Contains(underlyingType);

                    if (!isBasicType)
                    {
                        writer = handler.GetProperty("Writer").GetValue(null);
                        reader = handler.GetProperty("Reader").GetValue(null);
                    }

                    if (isBasicType ||
                        writer != null && reader != null)
                    {
                        // :: var hasValue = value.HasValue;
                        // :: binaryWriter.Write(hasValue);
                        // :: if (hasValue)
                        // ::     <case 1 or 2>
                        var writeHasValue = Expression.Variable(typeof(bool));
                        var writeValueExpr = Expression.Call(valueExpr, type.GetProperty("Value").GetMethod);
                        writeLocals.Add(writeHasValue);
                        writeExprs.Add(
                            Expression.Assign(
                                writeHasValue,
                                Expression.Call(valueExpr, type.GetProperty("HasValue").GetMethod)
                            ));
                        writeExprs.Add(
                            Expression.Call(
                                binaryWriter,
                            BasicTypeWriteMethods[typeof(bool)], writeHasValue
                            ));
                        writeExprs.Add(
                            Expression.IfThen(
                                writeHasValue,
                                isBasicType
                                ? Expression.Call(
                                    binaryWriter,
                                    BasicTypeWriteMethods[underlyingType],
                                    writeValueExpr)
                                : Expression.Call(
                                    Expression.Call(handler.GetProperty("Writer").GetMethod),
                                    writer.GetType().GetMethod("Invoke"),
                                    binaryWriter,
                                    writeValueExpr,
                                    writeContext)
                            ));

                        // :: var hasValue = binaryReader.ReadBoolean();
                        // :: if (hasValue)
                        // ::     <case 1 or 2>
                        var readHasValue = Expression.Variable(typeof(bool));
                        readLocals.Add(readHasValue);
                        readExprs.Add(
                            Expression.Assign(
                                readHasValue,
                                Expression.Call(binaryReader, BasicTypeReadMethods[typeof(bool)])
                            ));
                        readValueExpr = Expression.Variable(type);
                        readLocals.Add((ParameterExpression)readValueExpr);
                        readExprs.Add(
                            Expression.IfThen(
                                readHasValue,
                                Expression.Assign(
                                    readValueExpr,
                                    Expression.Convert(
                                        isBasicType
                                        ? Expression.Call(binaryReader, BasicTypeReadMethods[underlyingType])
                                        : Expression.Call(
                                            Expression.Call(handler.GetProperty("Reader").GetMethod),
                                            reader.GetType().GetMethod("Invoke"),
                                            binaryReader,
                                            readContext),
                                        type)
                                    )));
                        return true;
                    }
                }
            }

            // case 4. string, write:
            // [0x00][BinaryWriter.Write(value)] if not null,
            // [0xFF] if null
            if (type == typeof(string))
            {
                // :: if (value != null)
                // :: {
                // ::     binaryWriter.Write((byte)0x00);
                // ::     binaryWriter.Write(value);
                // :: }
                // :: else
                // :: {
                // ::     binaryWriter.Write((byte)0xFF);
                // :: }
                writeExprs.Add(Expression.IfThenElse(
                    Expression.NotEqual(valueExpr, Expression.Constant(null, type)),
                    Expression.Block(
                        Expression.Call(
                            binaryWriter,
                            BasicTypeWriteMethods[typeof(byte)],
                            Expression.Constant((byte)0x00, typeof(byte))
                        ),
                        Expression.Call(
                            binaryWriter,
                            BasicTypeWriteMethods[type],
                            valueExpr
                        )
                    ),
                    Expression.Call(
                        binaryWriter,
                        BasicTypeWriteMethods[typeof(byte)],
                        Expression.Constant((byte)0xFF, typeof(byte))
                    )
                ));

                // :: readValue = binaryReader.ReadByte() == (byte)0x00 ? binaryReader.ReadString() : null;
                readValueExpr = Expression.Variable(type);
                readLocals.Add((ParameterExpression) readValueExpr);
                readExprs.Add(Expression.Assign(
                    readValueExpr,
                    Expression.Condition(
                        Expression.Equal(
                            Expression.Call(binaryReader, BasicTypeReadMethods[typeof(byte)]),
                            Expression.Constant((byte) 0x00, typeof(byte))),
                        Expression.Call(binaryReader, BasicTypeReadMethods[typeof(string)]),
                        Expression.Constant(null, type)
                    )
                ));

                return true;
            }

            readValueExpr = null;
            return false;
        }

        /// <summary>
        /// Calls TryCreateExpressionForType. If it succeeds, add all expressions to the package.
        /// </summary>
        public static bool TryAddExpressionForType(Type type, HandlerExpressionPackage package, Expression valueExpr, out Expression readValueExpr)
        {
            if (TryCreateExpressionForType(type, valueExpr, package._writeBinaryWriter, package._readBinaryReader,
                package._writeContext, package._readContext,
                out var writeExprs, out var readExprs, out var writeLocals, out var readLocals, out var readValue))
            {
                package._writeExpressions.AddRange(writeExprs);
                package._readExpressions.AddRange(readExprs);
                package._writeLocals.AddRange(writeLocals);
                package._readLocals.AddRange(readLocals);
                readValueExpr = readValue;
                return true;
            }
            else
            {
                readValueExpr = null;
                return false;
            }
        }

        /// <summary>
        /// Throw an exception if the type is value type. This is used to reject value types that are not basic and have no handlers.
        /// </summary>
        public static void ThrowOnValueType(Type type)
        {
            if (type.IsValueType)
            {
                throw new Exception($"Value type {type.FullName} needs an handler.");
            }
        }
    }
}
