namespace migrator
{
    public class Migration
    {

        public Migration(string name, string query, int number)
        {
            Number = number;
            Name = name;
            Query = query;

        }

        public readonly int Number;

        public string Query { get; }

        public string Name { get; }
    }
}
