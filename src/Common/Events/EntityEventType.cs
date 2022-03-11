namespace Common
{
    public class EntityEventType
    {
        public static EntityEventType Added => new ("Added");

        public static EntityEventType Modified => new ("Modified");

        public static EntityEventType Deleted => new ("Deleted");

        public string Value { get; }

        private EntityEventType(string value)
        {
            Value = value;
        }
    }
}