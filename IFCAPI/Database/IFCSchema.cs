using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace IFCAPI.Database
{
    public class IFCSchema
    {
        protected Dictionary<string, string> ifcschema = new Dictionary<string, string>();
        public Dictionary<string, string> GetSchema(string ifcSchemaType)
        {
            setSchema(ifcSchemaType);
            return ifcschema;
        }
        public IFCSchema() { }
        public void setSchema(string ifcSchemaType)
        {
            ifcschema = new Dictionary<string, string>();

            if (ifcSchemaType.Contains("IFC4"))
            {
                StreamReader reader = new StreamReader(this.GetType().Assembly.GetManifestResourceStream("IFCAPI.Database.IFCSchema.IFC4.txt"));
                string line = "";
                while ((line = reader.ReadLine()) != null)
                {
                    string[] schemaStr = line.Split(':');
                    ifcschema.Add(schemaStr[0].ToUpper().Trim(), schemaStr[1].Trim());
                }
                return;
            }
            return;
        }
        //public async void SetSchema(Dictionary<string, string> listOfSchemaString, string ifcSchemaType)
        //{
        //    MongoClient server = new MongoClient("mongodb://" + sevrerName);
        //    var db = server.GetDatabase("IFCSystem");
        //    var col = db.GetCollection<BsonDocument>("IFCSchema");
        //    var filter = Builders<BsonDocument>.Filter.Eq("_SCHEMA", ifcSchemaType);
        //    await col.DeleteOneAsync(filter);
        //    BsonDocument IFCBson = new BsonDocument();
        //    IFCBson.Add("_SCHEMA", ifcSchemaType);
        //    foreach (KeyValuePair<string, string> aSchema in listOfSchemaString)
        //        IFCBson.Add(new BsonElement(aSchema.Key.ToString(), aSchema.Value.ToString()));
        //    await col.InsertOneAsync(IFCBson);
        //}
        //protected void GetDictionary(string ifcSchemaType)
        //{
        //    MongoClient server = new MongoClient("mongodb://" + sevrerName);
        //    var db = server.GetDatabase("IFCSystem");
        //    var col = db.GetCollection<BsonDocument>("IFCSchema");
        //    var filter = Builders<BsonDocument>.Filter.Eq("_SCHEMA", ifcSchemaType); //Builders<BsonDocument>.Filter.Eq("_SCHEMA", ifcSchemaType);                      
        //    var result = col.Find(filter).ToListAsync();
        //    //string test = "aaa";
        //    foreach (BsonDocument document in result.Result)
        //    {
        //        foreach (BsonElement bs in document)
        //        {
        //            ifcschema.Add(bs.Name, bs.Value.ToString());
        //        }
        //    }

        //    ifcschema.Remove("_SCHEMA");
        //}
    }

}
