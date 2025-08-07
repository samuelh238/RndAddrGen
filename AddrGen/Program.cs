using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace AddrGen
{
    internal class Program
    {
        private static Random rnd = new Random();
        private static Dictionary<int, (string, string)> _zip_CS = new Dictionary<int, (string, string)>();
        //private static readonly List<string> StreetSuffixes = new List<string> { "AVE", "BLVD", "CT", "DR", "GRV", "JCT", "LOOP", "PIKE", "RD", "WAY" };
        private static readonly List<string> StreetNames = File.ReadAllText("CommonStreetNames.txt").Split(Environment.NewLine).ToList();
        private static List<int>? zips = null;


        private static readonly Int64 NUM_OF_DATAPOINTS = 100_000_000;
        private static readonly Int64 CHUNK_SIZE = 100_000;

        private static readonly string outputFilePath = "RndAddr100MN_NoHeader.csv";

        static void Main(string[] args)
        {
            PopulateZipCS_Dict();
            List<AddrInfo> addrInfoToWrite = new List<AddrInfo>();
            bool headersWritten = false;
            for (long i = 0; i < NUM_OF_DATAPOINTS; i++)
            {
                addrInfoToWrite.Add(GenAddr());

                if (addrInfoToWrite.Count < CHUNK_SIZE)
                    continue;

                WriteAddrData(ref addrInfoToWrite, headersWritten);
                headersWritten = true;
                addrInfoToWrite.Clear();
            }
        }


        public static AddrInfo GenAddr()
        {

            AddrInfo addrInfo = new AddrInfo();
            StringBuilder sb = new StringBuilder();
            

            string streetNum = rnd.Next(1, 99999).ToString();


            //string street name
            int streetNameIndex = rnd.Next(1, StreetNames.Count);
            string streetName = StreetNames[streetNameIndex];

            //string street suffix
            //int streetSuffixIndex = rnd.Next(1, StreetSuffixes.Count);
            //string streetSuffix = StreetSuffixes[streetSuffixIndex];

            sb.AppendJoin(" ",streetNum,streetName);
            addrInfo.PrimaryAddress = sb.ToString();

            //random Zip state, City, 
            int zipIndex = rnd.Next(1, zips!.Count);
            addrInfo.Zip = zips[zipIndex].ToString().PadLeft(5,'0');
            addrInfo.Zip4 = rnd.Next(1,9999).ToString().PadLeft(4,'0');
            addrInfo.City = _zip_CS[zips[zipIndex]].Item1;
            addrInfo.State = _zip_CS[zips[zipIndex]].Item2;

            return addrInfo;
        }

        public static void WriteAddrData(ref List<AddrInfo> addrs, bool append)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                // Don't write the header again.
                HasHeaderRecord = false,
                NewLine = "\n"
            };
            if (!append)
            {
                // Write to a file.
                using (var writer = new StreamWriter(outputFilePath))
                using (var csv = new CsvWriter(writer, config))
                {
                    csv.WriteRecords(addrs);
                }
            }
            else
            {
                // Append to the file.
                
                using (var stream = File.Open(outputFilePath, FileMode.Append))
                using (var writer = new StreamWriter(stream))
                using (var csv = new CsvWriter(writer, config))
                {
                    csv.WriteRecords(addrs);
                }
            }
            
        }
        public static void PopulateZipCS_Dict()
        {
            
            using (var reader = new StreamReader("ZIP_Locale_Detail.csv"))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                csv.Context.RegisterClassMap<US_Zip_DetailsClassMap>();
                var records = csv.GetRecords<US_Zip_Details>().ToList();

                foreach (var item in records)
                {
                    int zip = int.Parse(item.DELIVERYZIPCODE);
                    if (_zip_CS.ContainsKey(zip))
                        continue;
                    _zip_CS.Add(zip, (item.PHYSICALCITY, item.PHYSICALSTATE));
                }
            }
            zips = _zip_CS.Keys.ToList();
        }

        public class AddrInfo
        {
            public string PrimaryAddress { get; set; } = string.Empty;
            public string City { get; set; }
            public string State { get; set; }
            public string Zip {  get; set; }
            public string Zip4 { get; set; }
        }


        public class US_Zip_Details
        {
            public string AREANAME { get; set; }
            public string AREACODE { get; set; }
            public string DISTRICTNAME { get; set; }
            public string DISTRICTNO { get; set; }
            public string DELIVERYZIPCODE { get; set; }
            public string LOCALENAME { get; set; }
            public string PHYSICALDELVADDR { get; set; }
            public string PHYSICALCITY { get; set; }
            public string PHYSICALSTATE { get; set; }
            public string PHYSICALZIP { get; set; }
            public string PHYSICALZIP4 { get; set; }
        }

        public class US_Zip_DetailsClassMap : ClassMap<US_Zip_Details>
        {
            public US_Zip_DetailsClassMap()
            {
                Map(m => m.AREANAME).Name("AREA NAME");
                Map(m => m.AREACODE).Name("AREA CODE");
                Map(m => m.DISTRICTNAME).Name("DISTRICT NAME");
                Map(m => m.DISTRICTNO).Name("DISTRICT NO");
                Map(m => m.DELIVERYZIPCODE).Name("DELIVERY ZIPCODE");
                Map(m => m.LOCALENAME).Name("LOCALE NAME");
                Map(m => m.PHYSICALDELVADDR).Name("PHYSICAL DELV ADDR");
                Map(m => m.PHYSICALCITY).Name("PHYSICAL CITY");
                Map(m => m.PHYSICALSTATE).Name("PHYSICAL STATE");
                Map(m => m.PHYSICALZIP).Name("PHYSICAL ZIP");
                Map(m => m.PHYSICALZIP4).Name("PHYSICAL ZIP 4");
            }
        }

    }
}
