using System;
using System.CodeDom.Compiler;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JsonHierarchy
{
    class Program
    {
        static void Main(string[] args)
        {
            // Create a JSON string for a given person.
            var json = JsonConvert.SerializeObject(CreatePerson(), Formatting.Indented);
            Console.WriteLine("JSON");
            Console.WriteLine("----");
            Console.WriteLine();
            Console.WriteLine(json);
            Console.WriteLine();

            // Now deserialize the JSON string into an instance of Person.
            // We're registering the ModeOfTransportationJsonConverter which
            // knows how to create a Bike or Car instance, given a fragment
            // of JSON.
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new ModeOfTransportationJsonConverter());

            var person = JsonConvert.DeserializeObject<Person>(json, settings);
            Console.WriteLine("Object");
            Console.WriteLine("------");
            Console.WriteLine();
            Write(person);
        }

        static Person CreatePerson()
        {
            return new Person
            {
                Name = "Immo",
                ModeOfTransportations = new ModeOfTransportation[]
                {
                    new Bike
                    {
                        Brand = "Specialized",
                        WeightInKilograms = 7.6f,
                        Size = 56
                    },
                    new Car
                    {
                        Brand = "BMW",
                        WeightInKilograms = 1823.2f,
                        NumberOfDoors = 4
                    }
                }
            };
        }

        static void Write(Person person)
        {
            var writer = new IndentedTextWriter(Console.Out);
            Write(writer, person);
        }

        static void Write(IndentedTextWriter textWriter, Person person)
        {
            textWriter.WriteLine($"Name: {person.Name}");
            textWriter.WriteLine("ModeOfTransportations {");
            textWriter.Indent++;
            foreach (var mode in person.ModeOfTransportations)
            {
                Write(textWriter, mode);
            }
            textWriter.Indent--;
            textWriter.WriteLine("}");
        }

        static void Write(IndentedTextWriter textWriter, ModeOfTransportation mode)
        {
            textWriter.WriteLine($"{mode.Kind} {{");
            textWriter.Indent++;
            textWriter.WriteLine($"Brand: {mode.Brand}");
            textWriter.WriteLine($"WeightInKilograms: {mode.WeightInKilograms}");

            switch (mode)
            {
                case Bike bike:
                    Write(textWriter, bike);
                    break;
                case Car car:
                    Write(textWriter, car);
                    break;
                default:
                    throw new Exception("Unexpected mode of transportation: " + mode.GetType());
            }
            textWriter.Indent--;
            textWriter.WriteLine("}");
        }

        private static void Write(IndentedTextWriter textWriter, Bike bike)
        {
            textWriter.WriteLine($"Size: {bike.Size}");
        }

        private static void Write(IndentedTextWriter textWriter, Car car)
        {
            textWriter.WriteLine($"NumberOfDoors: {car.NumberOfDoors}");
        }
    }
}

class Person
{
    public string Name { get; set; }
    public ModeOfTransportation[] ModeOfTransportations { get; set; }
}

abstract class ModeOfTransportation
{
    public string Kind => GetType().Name;
    public string Brand { get; set; }
    public float WeightInKilograms { get; set; }
}

class Bike : ModeOfTransportation
{
    public int Size { get; set; }
}

class Car : ModeOfTransportation
{
    public int NumberOfDoors { get; set; }
}

public class ModeOfTransportationJsonConverter : JsonConverter
{
    public override bool CanWrite => false;

    public override bool CanConvert(Type objectType)
    {
        return typeof(ModeOfTransportation).IsAssignableFrom(objectType);
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        throw new NotSupportedException("Custom converter should only be used while deserializing.");
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        var obj = JObject.Load(reader);
        if (obj == null)
            return null;

        var type = GetType(obj);
        if (type == null)
            throw new Exception("Unexpected mode of transportation: " + obj);

        var result = Activator.CreateInstance(type);
        serializer.Populate(obj.CreateReader(), result);
        return result;
    }

    private static Type GetType(JObject obj)
    {
        var kind = obj.Value<string>("Kind");

        switch (kind)
        {
            case "Car":
                return typeof(Car);
            case "Bike":
                return typeof(Bike);
            default:
                return null;
        }
    }
}
