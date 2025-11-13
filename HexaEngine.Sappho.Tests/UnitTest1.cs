namespace HexaEngine.Sappho.Tests
{
    using System;

    public class SerializationTests
    {
        private SapphoSerializationContext? context;

        [SetUp]
        public void Setup()
        {
            context = new SapphoSerializationContext();
        }

        [Test]
        public void TestPrimitiveTypes()
        {
            var obj = new PrimitiveTestObject
            {
                ByteValue = 42,
                SByteValue = -42,
                ShortValue = -1234,
                UShortValue = 1234,
                IntValue = -123456,
                UIntValue = 123456,
                LongValue = -1234567890L,
                ULongValue = 1234567890UL,
                FloatValue = 3.14f,
                DoubleValue = 2.718281828,
                BoolValue = true,
                CharValue = 'A'
            };

            var result = SerializeAndDeserialize(obj);

            Assert.Multiple(() =>
            {
                Assert.That(result.ByteValue, Is.EqualTo(obj.ByteValue));
                Assert.That(result.SByteValue, Is.EqualTo(obj.SByteValue));
                Assert.That(result.ShortValue, Is.EqualTo(obj.ShortValue));
                Assert.That(result.UShortValue, Is.EqualTo(obj.UShortValue));
                Assert.That(result.IntValue, Is.EqualTo(obj.IntValue));
                Assert.That(result.UIntValue, Is.EqualTo(obj.UIntValue));
                Assert.That(result.LongValue, Is.EqualTo(obj.LongValue));
                Assert.That(result.ULongValue, Is.EqualTo(obj.ULongValue));
                Assert.That(result.FloatValue, Is.EqualTo(obj.FloatValue).Within(0.0001f));
                Assert.That(result.DoubleValue, Is.EqualTo(obj.DoubleValue).Within(0.0000001));
                Assert.That(result.BoolValue, Is.EqualTo(obj.BoolValue));
                Assert.That(result.CharValue, Is.EqualTo(obj.CharValue));
            });
        }

        [Test]
        public void TestStringTypes()
        {
            var obj = new StringTestObject
            {
                SimpleString = "Hello, World!",
                EmptyString = "",
                UnicodeString = "Hello 世界 🌍",
                LongString = new string('x', 10000)
            };

            var result = SerializeAndDeserialize(obj);

            Assert.Multiple(() =>
            {
                Assert.That(result.SimpleString, Is.EqualTo(obj.SimpleString));
                Assert.That(result.EmptyString, Is.EqualTo(obj.EmptyString));
                Assert.That(result.UnicodeString, Is.EqualTo(obj.UnicodeString));
                Assert.That(result.LongString, Is.EqualTo(obj.LongString));
            });
        }

        [Test]
        public void TestEnumTypes()
        {
            var obj = new EnumTestObject
            {
                ByteEnum = TestByteEnum.Value2,
                IntEnum = TestIntEnum.ValueB,
                LongEnum = TestLongEnum.Large
            };

            var result = SerializeAndDeserialize(obj);

            Assert.Multiple(() =>
            {
                Assert.That(result.ByteEnum, Is.EqualTo(obj.ByteEnum));
                Assert.That(result.IntEnum, Is.EqualTo(obj.IntEnum));
                Assert.That(result.LongEnum, Is.EqualTo(obj.LongEnum));
            });
        }

        [Test]
        public void TestEmptyObject()
        {
            var obj = new EmptyObject();
            var result = SerializeAndDeserialize(obj);
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void TestStructSerialization()
        {
            var obj = new StructTestObject
            {
                StructValue = new MetadataStruct
                {
                    Version = 42,
                    Created = DateTime.Now.Ticks
                }
            };

            var result = SerializeAndDeserialize(obj);

            Assert.Multiple(() =>
            {
                Assert.That(result.StructValue.Version, Is.EqualTo(obj.StructValue.Version));
                Assert.That(result.StructValue.Created, Is.EqualTo(obj.StructValue.Created));
            });
        }

        [Test]
        public void TestPolymorphism()
        {
            // Register types in context for polymorphic deserialization
            context!.RegisterType<BaseAnimal>(BaseAnimal.SapphoTypeId);
            context.RegisterType<Dog>(Dog.SapphoTypeId);
            context.RegisterType<Cat>(Cat.SapphoTypeId);

            // Create a container with polymorphic animals
            var obj = new AnimalContainer
            {
                Name = "Zoo",
                Animals = new BaseAnimal[]
                {
                    new Dog { Name = "Buddy", Species = "Canine", Breed = "Golden Retriever", IsGoodBoy = true },
                    new Cat { Name = "Whiskers", Species = "Feline", LivesRemaining = 7, IsSleeping = true },
                    new Dog { Name = "Max", Species = "Canine", Breed = "German Shepherd", IsGoodBoy = true }
                }
            };

            var result = SerializeAndDeserialize(obj);

            Assert.Multiple(() =>
            {
                Assert.That(result.Name, Is.EqualTo("Zoo"));
                Assert.That(result.Animals, Is.Not.Null);
                Assert.That(result.Animals!.Length, Is.EqualTo(3));

                // First animal should be a Dog
                Assert.That(result.Animals[0], Is.InstanceOf<Dog>());
                var dog1 = (Dog)result.Animals[0];
                Assert.That(dog1.Name, Is.EqualTo("Buddy"));
                Assert.That(dog1.Species, Is.EqualTo("Canine"));
                Assert.That(dog1.Breed, Is.EqualTo("Golden Retriever"));
                Assert.That(dog1.IsGoodBoy, Is.True);

                // Second animal should be a Cat
                Assert.That(result.Animals[1], Is.InstanceOf<Cat>());
                var cat = (Cat)result.Animals[1];
                Assert.That(cat.Name, Is.EqualTo("Whiskers"));
                Assert.That(cat.Species, Is.EqualTo("Feline"));
                Assert.That(cat.LivesRemaining, Is.EqualTo(7));
                Assert.That(cat.IsSleeping, Is.True);

                // Third animal should be a Dog
                Assert.That(result.Animals[2], Is.InstanceOf<Dog>());
                var dog2 = (Dog)result.Animals[2];
                Assert.That(dog2.Name, Is.EqualTo("Max"));
                Assert.That(dog2.Breed, Is.EqualTo("German Shepherd"));
            });
        }

        [Test]
        public void TestPolymorphicNull()
        {
            context!.RegisterType<BaseAnimal>(BaseAnimal.SapphoTypeId);

            var obj = new AnimalContainer
            {
                Name = "Empty Zoo",
                Animals = null
            };

            var result = SerializeAndDeserialize(obj);

            Assert.Multiple(() =>
            {
                Assert.That(result.Name, Is.EqualTo("Empty Zoo"));
                Assert.That(result.Animals, Is.Null);
            });
        }

        [Test]
        public void TestPolymorphicSingleInheritance()
        {
            context!.RegisterType<Vehicle>(Vehicle.SapphoTypeId);
            context.RegisterType<Car>(Car.SapphoTypeId);

            var obj = new VehicleOwner
            {
                OwnerName = "John Doe",
                OwnedVehicle = new Car
                {
                    Make = "Toyota",
                    Model = "Camry",
                    Year = 2024,
                    NumberOfDoors = 4
                }
            };

            var result = SerializeAndDeserialize(obj);

            Assert.Multiple(() =>
            {
                Assert.That(result.OwnerName, Is.EqualTo("John Doe"));
                Assert.That(result.OwnedVehicle, Is.Not.Null);
                Assert.That(result.OwnedVehicle, Is.InstanceOf<Car>());

                var car = (Car)result.OwnedVehicle!;
                Assert.That(car.Make, Is.EqualTo("Toyota"));
                Assert.That(car.Model, Is.EqualTo("Camry"));
                Assert.That(car.Year, Is.EqualTo(2024));
                Assert.That(car.NumberOfDoors, Is.EqualTo(4));
            });
        }

        [Test]
        public void TestCyclicReference()
        {
            var obj = new CyclicReference();
            obj.Reference = obj; // Create cyclic reference
            var result = SerializeAndDeserialize(obj);
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Reference, Is.Not.Null);
                Assert.That(ReferenceEquals(result, result.Reference), Is.True);
            });
        }

        private unsafe T SerializeAndDeserialize<T>(T obj) where T : class, ISapphoSerializable<T>
        {
            using SapphoWriter writer = new(context!);
            writer.WriteObject(obj);

            SapphoReader reader = new(writer.Buffer, writer.Count, context!);
            var result = reader.ReadObject<T>();
            return result!;
        }
    }

    // Test Data Types

    [SapphoObject]
    public partial class PrimitiveTestObject
    {
        public byte ByteValue { get; set; }

        public sbyte SByteValue { get; set; }

        public short ShortValue { get; set; }

        public ushort UShortValue { get; set; }

        public int IntValue { get; set; }

        public uint UIntValue { get; set; }

        public long LongValue { get; set; }

        public ulong ULongValue { get; set; }

        public float FloatValue { get; set; }

        public double DoubleValue { get; set; }

        public bool BoolValue { get; set; }

        public char CharValue { get; set; }
    }

    [SapphoObject]
    public partial class StringTestObject
    {
        public string SimpleString { get; set; } = string.Empty;
        public string EmptyString { get; set; } = string.Empty;
        public string UnicodeString { get; set; } = string.Empty;
        public string LongString { get; set; } = string.Empty;
    }

    public enum TestByteEnum : byte
    {
        Value1 = 1,
        Value2 = 2,
        Value3 = 3
    }

    public enum TestIntEnum
    {
        ValueA = 100,
        ValueB = 200,
        ValueC = 300
    }

    public enum TestLongEnum : long
    {
        Small = 1L,
        Medium = 1000000L,
        Large = 1000000000000L
    }

    [SapphoObject]
    public partial class EnumTestObject
    {
        public TestByteEnum ByteEnum { get; set; }
        public TestIntEnum IntEnum { get; set; }
        public TestLongEnum LongEnum { get; set; }
    }

    public struct MetadataStruct
    {
        public int Version { get; set; }
        public long Created { get; set; }
    }

    [SapphoObject]
    public partial class EmptyObject
    {
    }

    [SapphoObject]
    public partial class StructTestObject
    {
        public MetadataStruct StructValue { get; set; }
    }

    // Polymorphism Test Types

    [SapphoObject]
    public partial class BaseAnimal
    {
        public string Name { get; set; } = string.Empty;
        public string Species { get; set; } = string.Empty;
    }

    [SapphoObject]
    public partial class Dog : BaseAnimal
    {
        public string Breed { get; set; } = string.Empty;
        public bool IsGoodBoy { get; set; }
    }

    [SapphoObject]
    public partial class Cat : BaseAnimal
    {
        public int LivesRemaining { get; set; }
        public bool IsSleeping { get; set; }
    }

    [SapphoObject]
    public partial class AnimalContainer
    {
        public string Name { get; set; } = string.Empty;

        [SapphoPolymorphic]
        public BaseAnimal[]? Animals { get; set; }
    }

    [SapphoObject]
    public partial class Vehicle
    {
        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int Year { get; set; }
    }

    [SapphoObject]
    public partial class Car : Vehicle
    {
        public int NumberOfDoors { get; set; }
    }

    [SapphoObject]
    public partial class VehicleOwner
    {
        public string OwnerName { get; set; } = string.Empty;

        [SapphoPolymorphic]
        public Vehicle? OwnedVehicle { get; set; }
    }

    [SapphoObject]
    public partial class CyclicReference
    {
        public CyclicReference? Reference { get; set; }
    }
}