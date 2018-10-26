using System;
using System.Collections.Generic;
using System.Text;
using LiteDB;
using System.IO;
using System.Linq;

namespace IFCAPI.Database
{
    public class IFCDatabase
    {
        private static readonly Lazy<IFCDatabase> IFCDB = new Lazy<IFCDatabase>(() => new IFCDatabase());
        private IFCDatabase() { }
        public static IFCDatabase Instance { get { return IFCDB.Value; } }

        private LiteDatabase IfcDatabase = new LiteDatabase( "filename=" + Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            "Database","EmbeddedDB.db") + "; journal=false");
        
        //public void initDB(string dbPath)
        //{
        //    IfcDatabase = new LiteDatabase(dbPath);
        //}
        private string setColl(string collName)
        {
            try
            {
                if (IfcDatabase.CollectionExists(collName))
                    IfcDatabase.DropCollection(collName);
                IfcDatabase.GetCollection(collName);
                return "Create collection successful.";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        public string loadIFCFile(string filePath,string collName)
        {
            try
            {
                
                //設定集合名稱
                setColl(collName);

                LiteCollection<BsonDocument> coll = IfcDatabase.GetCollection(collName);
                IFCReader reader = new IFCReader(filePath, IfcDatabase.GetCollection(collName));

                return reader.finishMessage;
            }
            catch(Exception ex)
            {
                return ex.Message;
            }
        }
    }
}
