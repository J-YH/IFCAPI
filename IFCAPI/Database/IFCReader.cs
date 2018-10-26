using System;
using System.Collections.Generic;
using LiteDB;
using System.Collections;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

namespace IFCAPI.Database
{
    public class IFCReader
    {
        public string finishMessage = "";
        Dictionary<string, string> allIFCSchema = new Dictionary<string, string>();
        Dictionary<string, string> typeOfP21id = new Dictionary<string, string>();
        Dictionary<string, List<string>> _MappingTable = new Dictionary<string, List<string>>();
        BsonDocument HEAD = new BsonDocument();
        public IFCReader()
        {

        }
        public IFCReader(string filePath, LiteCollection<BsonDocument> liteCollection)
        {

            finishMessage += "讀取" + filePath + Environment.NewLine;
            finishMessage += "存入" + liteCollection.Name + Environment.NewLine;
            finishMessage = ReadFromIFCWriteToLiteDB(filePath, liteCollection);
        }


        private string ReadFromIFCWriteToLiteDB(string filePath, LiteCollection<BsonDocument> liteCollection)
        {
            // allIFCSchema = new IFC2x3Schema(serverName).getSchema();
            List<BsonDocument> IFCBsonArray = combineIFC(loadIFCFile(filePath));
            HEAD.Add("_EntityName", "_HEAD");
            IFCBsonArray.Add(HEAD);         //加入標頭檔
            IFCBsonArray.Add(replacedata);  //加入取代檔
            IFCBsonArray.Add(getMappingTable());    //加入MappingTable
            finishMessage += "壓縮後資料筆數：" + IFCBsonArray.Count.ToString() + Environment.NewLine;

            //REX測試暫時不insert
            //bulkInsert(liteCollection, IFCBsonArray);

            IEnumerable<BsonDocument> IFCBsonValue = IFCBsonArray;
            bulkInsert(liteCollection, IFCBsonValue);

            return finishMessage;
        }
        private BsonDocument getMappingTable()
        {
            BsonDocument BD = new BsonDocument();
            BD.Add("_EntityName", "_MAPPINGTABLE");
            foreach (var BE in _MappingTable)
            {
                BsonArray arr = new BsonArray();
                foreach (string arrValue in BE.Value)
                    arr.Add(new BsonValue(arrValue));
                BD.Add(BE.Key, arr);
            }
            return BD;
        }
        public string GetLog()
        {
            if (finishMessage == "")
                return "Do nothing. No log.";
            else
                return finishMessage;
        }

        private  void bulkInsert(LiteCollection<BsonDocument> liteCollection, List<BsonDocument> IFCBson)
        {
            //用List<>
            liteCollection.Insert(IFCBson);
            liteCollection.EnsureIndex("_P21id");
            liteCollection.EnsureIndex("_EntityName");
            liteCollection.EnsureIndex("GlobalId");
        }
        private void bulkInsert(LiteCollection<BsonDocument> liteCollection, IEnumerable<BsonDocument> IFCBson)
        {
            //用List<>
            liteCollection.Insert(IFCBson);
            liteCollection.EnsureIndex("_P21id");
            liteCollection.EnsureIndex("_EntityName");
            liteCollection.EnsureIndex("GlobalId");
        }
        BsonDocument replacedata = new BsonDocument();

        private List<BsonDocument> combineIFC(List<IFCData> allIFCRow)
        {
            List<BsonDocument> allIFCBson = new List<BsonDocument>();

            IFCData lastIFC = new IFCData();

            replacedata.Add("_id", Guid.NewGuid().ToString());
            replacedata.Add("_EntityName", "_REPLACE"); //REX新增
            BsonArray array1 = new BsonArray();

            int notEqual = 0;
            int isEqual = 0;

            string tmpStrforReplace = "";
            foreach (IFCData aIFC in allIFCRow.OrderBy(ifc => (ifc.ifcName + ifc.ifcContent)))
            {
                if ((aIFC.ifcName + aIFC.ifcContent) != (lastIFC.ifcName + lastIFC.ifcContent))
                {
                    allIFCBson.Add(getIFCBson(aIFC.ifcP21ID, aIFC.ifcName, aIFC.ifcContent));
                    lastIFC = aIFC;
                    if (tmpStrforReplace.Length != 0)
                    {
                        array1.Add(tmpStrforReplace);
                        tmpStrforReplace = "";
                    }
                    notEqual++;
                }
                else
                {
                    if (tmpStrforReplace.Length == 0)
                        tmpStrforReplace = lastIFC.ifcP21ID;
                    tmpStrforReplace += "," + aIFC.ifcP21ID;
                    isEqual++;
                }
            }
            if (tmpStrforReplace != "")
                array1.Add(tmpStrforReplace);
            replacedata.Add("_replace", array1);
            finishMessage += "直接儲存筆數：" + notEqual.ToString() + Environment.NewLine + "放入Replace資料筆數：" + isEqual.ToString() + Environment.NewLine;
            //AllIfcBsonData.Add(replacedata);
            return allIFCBson;
        }


        private BsonDocument getIFCBson(string ifchash, string ifcName, string ifcContent)
        {
            ifcName = ifcName.Trim();
            int indexInSchema = 0;                                  //for check
            ArrayList aIFCRowData = new ArrayList();                //建立用來儲存一筆ifc資料的container
            //Dictionary<string, string> allIFCSchema = getSchema(@"ifcFileField.txt");
            string ifcSchema = allIFCSchema[ifcName.Trim()].ToString();

            string[] schemaSplit = ifcSchema.Split(',');
            foreach (string s in schemaSplit) //添加屬性Pair
            {
                aIFCRowData.Add(new IFCPair(s, ""));            //裡面放自訂義Pair
                indexInSchema++;                                    //for check
            }
            string[] contentSplit = ifcContent.Split(',');          //處理IFC原始字串
            string tmpString = "";                                  //用來"接"字串
            int indexOfSchema = 0;                                  //用來對應aIFCRowData裡面的index, 跳過第一個type
            bool ifArrayValue = false;
            bool ifStringValue = false;
            try
            {
                foreach (string s in contentSplit)
                {
                    if (ifArrayValue || ifStringValue)
                        tmpString += "," + s;                             //把逗點補回去
                    else
                        tmpString += s;
                    if (s[0] == '(')                                    //ifc的值是陣列                
                    {
                        if (s[s.Length - 1] == ')')                     //結尾就是')'
                        {
                            ((IFCPair)aIFCRowData[indexOfSchema]).ifcValue = tmpString;
                            tmpString = "";
                            indexOfSchema++;
                        }
                        else
                            ifArrayValue = true;
                    }
                    else if (s[0] == '\'')
                    {
                        if (s[s.Length - 1] == '\'')                     //結尾就是'\''
                        {
                            ((IFCPair)aIFCRowData[indexOfSchema]).ifcValue = tmpString;
                            tmpString = "";
                            indexOfSchema++;
                        }
                        else
                            ifStringValue = true;
                    }
                    else
                    {
                        if (ifArrayValue && !ifStringValue)
                        {
                            if (s[s.Length - 1] == ')')                //陣列結尾         else 僅連結陣列字串(開頭就做了)
                            {
                                ifArrayValue = false;
                                ((IFCPair)aIFCRowData[indexOfSchema]).ifcValue = tmpString;
                                tmpString = "";
                                indexOfSchema++;
                            }
                        }
                        else if (!ifArrayValue && ifStringValue)       //only one situation could happened   array or string
                        {
                            if (s[s.Length - 1] == '\'')               //字串結尾         else 僅連結陣列字串(開頭就做了)
                            {
                                if (s.Length > 2)
                                    if (s[s.Length - 2] == '\\')        //s的長度如果小於2會出現runtime error
                                        continue;
                                ifStringValue = false;
                                ((IFCPair)aIFCRowData[indexOfSchema]).ifcValue = tmpString;
                                tmpString = "";
                                indexOfSchema++;
                            }
                        }
                        else
                        {
                            ((IFCPair)aIFCRowData[indexOfSchema]).ifcValue = tmpString;
                            indexOfSchema++;
                            tmpString = "";
                        }
                    }
                }
            }
            catch (Exception exp)
            {
                //cannotCreateBsonObj += ifcName + "\r\n";
                //MessageBox.Show(exp.ToString());
            }
            if (indexInSchema != indexOfSchema)                     //檢核
                finishMessage += " Entity: " + ifcName +
                    " 規格表 / 實際 : " + indexInSchema.ToString() +
                    " / " + indexOfSchema.ToString() + " ; ";
            BsonDocument ifcBson = new BsonDocument();
            ifcBson.Add("_id", Guid.NewGuid().ToString());          //加入ID
            ifcBson.Add("_P21id", ifchash);                         //加入Hash
            ifcBson.Add("_EntityName", ifcName);
            foreach (IFCPair aPair in aIFCRowData)
            {
                if (aPair.ifcKey != "_id")
                {
                    if (aPair.ifcValue == "")
                        ifcBson.Add(aPair.ifcKey, aPair.ifcValue);
                    else if (aPair.ifcValue.Substring(0, 1) != "(")         //非陣列
                    {
                        ifcBson.Add(aPair.ifcKey, aPair.ifcValue);
                        if (aPair.ifcValue[0] == '#')                       //加入mapping table
                        {
                            string nextLevel = typeOfP21id[aPair.ifcValue];
                            if (_MappingTable.ContainsKey(nextLevel))
                                if (_MappingTable[nextLevel].Contains(ifcName))     //不知道為什麼不這樣寫不行
                                { }
                                else
                                    _MappingTable[nextLevel].Add(ifcName);
                            else
                                _MappingTable.Add(nextLevel, new List<string>() { ifcName });
                        }
                    }
                    else                                                    //陣列
                    {
                        List<string> arrayValue = cutString(aPair.ifcValue.Substring(1, aPair.ifcValue.Length - 2), ',');
                        BsonArray ba = new BsonArray();
                        foreach (string s in arrayValue)
                        {
                            ba.Add(s);
                            if (s.Length > 0)
                                if (s[0] == '#')
                                {
                                    string nextLevel = typeOfP21id[s];
                                    if (_MappingTable.ContainsKey(nextLevel))
                                        if (_MappingTable[nextLevel].Contains(ifcName))     //不知道為什麼不這樣寫不行
                                        { }
                                        else
                                            _MappingTable[nextLevel].Add(ifcName);
                                    else
                                        _MappingTable.Add(nextLevel, new List<string>() { ifcName });
                                }
                        }
                        ifcBson.Add(aPair.ifcKey, ba);
                    }
                }
            }
            return ifcBson;
        }

        //把字串用符號切開，並留意是否存在' '之中，只能用在已「接好」的String上，不能用在一開始判斷時
        private List<string> cutString(string values, char symbol)
        {
            List<string> returnList = new List<string>();
            if (!values.Contains(symbol))
            {
                returnList.Add(values);
                return returnList;
            }
            string[] arrayValue = values.Split(symbol);
            bool ifArrayValue = false;                                  //二維陣列考量，但目前沒有看到sample
            bool ifStringValue = false;
            string tmpString = "";

            foreach (string s in arrayValue)
            {
                if (s == "")
                    continue;
                if (ifArrayValue || ifStringValue)
                    tmpString += "," + s;                             //在二維陣列中，或在字串中把逗點補回去
                else
                    tmpString += s;
                if (s[0] == '(')                                      //ifc的值是二維陣列                
                {
                    if (s[s.Length - 1] == ')')                       //結尾就是')'
                    {
                        returnList.Add(tmpString);
                        tmpString = "";
                    }
                    else
                        ifArrayValue = true;
                }
                else if (s[0] == '\'')
                {
                    if (s[s.Length - 1] == '\'')                      //結尾就是'\''
                    {
                        returnList.Add(tmpString);
                        tmpString = "";
                    }
                    else
                        ifStringValue = true;
                }
                else
                {
                    if (ifArrayValue && !ifStringValue)
                    {
                        if (s[s.Length - 1] == ')')                //陣列結尾         else 僅連結陣列字串(開頭就做了)
                        {
                            ifArrayValue = false;
                            returnList.Add(tmpString);
                            tmpString = "";
                        }
                    }
                    else if (!ifArrayValue && ifStringValue)       //only one situation could happened   array or string
                    {
                        if (s[s.Length - 1] == '\'')               //字串結尾         else 僅連結陣列字串(開頭就做了)
                        {
                            if (s.Length > 2)
                                if (s[s.Length - 2] == '\\')        //s的長度如果小於2會出現runtime error
                                    continue;
                            ifStringValue = false;
                            returnList.Add(tmpString);
                            tmpString = "";
                        }
                    }
                    else
                    {
                        returnList.Add(tmpString);
                        tmpString = "";
                    }
                }
            }
            return returnList;
        }

        private List<IFCData> loadIFCFile(string filePathString)
        {
            List<IFCData> allIFCRow = new List<IFCData>();
            StreamReader sr = new StreamReader(filePathString);
            string tmp = "";
            int countOfStream = 0;      //行數計算
            int countOfAllString = 0;   //字元計算
            string lastString = "";     //結尾不為;的上一行

            if (!sr.EndOfStream)
                HEAD.Add("_ISO", sr.ReadLine());
            while (!sr.EndOfStream)
            {
                tmp = lastString + sr.ReadLine();
                //跳過/* */註解行
                if (tmp == "" || (tmp.Substring(0, 2) == "/*" && tmp.Substring(tmp.Length - 2) == "*/"))
                    continue;
                if (tmp[tmp.Length - 1] != ';')
                {
                    lastString = tmp;
                    continue;           //跳下一行
                }

                countOfStream++;
                countOfAllString += tmp.Length;
                if (tmp.Contains('=') && tmp.Contains(')') && tmp.Contains('('))    //表示完整的一行ifc資料
                {
                    string ifcHash = tmp.Substring(0, tmp.IndexOf('='));
                    string ifcName = tmp.Substring(tmp.IndexOf('=') + 1, tmp.IndexOf('(') - tmp.IndexOf('=') - 1);
                    string ifcContent = tmp.Substring(tmp.IndexOf('(') + 1, tmp.LastIndexOf(')') - tmp.IndexOf('(') - 1);
                    allIFCRow.Add(new IFCData(ifcHash, ifcName.Trim(), ifcContent));   //放入容器中
                    typeOfP21id.Add(ifcHash.Trim(), ifcName.Trim());                          //用來對照p21id的item名稱
                }   //後面處理head
                else if (tmp.Contains("FILE_DESCRIPTION"))
                    HEAD.Add("_FILE_DESCRIPTION", tmp.Substring(tmp.IndexOf("FILE_DESCRIPTION"), tmp.IndexOf(");") - tmp.IndexOf("FILE_DESCRIPTION") + 1));
                else if (tmp.Contains("FILE_NAME"))
                    HEAD.Add("_FILE_NAME", tmp.Substring(tmp.IndexOf("FILE_NAME"), tmp.IndexOf(");") - tmp.IndexOf("FILE_NAME") + 1));
                else if (tmp.Contains("FILE_SCHEMA"))
                {
                    string schemaType = tmp.Substring(tmp.IndexOf("FILE_SCHEMA"), tmp.IndexOf(");") - tmp.IndexOf("FILE_SCHEMA") + 1);
                    HEAD.Add("_FILE_SCHEMA", schemaType);
                    //讀取schema
                    finishMessage += "使用" + schemaType + Environment.NewLine;
                    schemaType = schemaType.Substring(schemaType.IndexOf("IFC"), schemaType.IndexOf("\'))") - schemaType.IndexOf("IFC"));
                    IFCSchema schema = new IFCSchema();
                    allIFCSchema = schema.GetSchema(schemaType);

                }
                lastString = "";        //處理完畢，清空上一行
            }
            //countOfIFCData.Text = allIFCRow.Count.ToString();
            //ifcContents.AppendText("總共" + countOfStream.ToString() + "行 資料長度：" + countOfAllString.ToString() + "個字元\r\n");
            //ifcContents.AppendText("總共" + allIFCRow.Count.ToString() + "筆 IFC資料\r\n");

            finishMessage += "未壓縮前資料筆數：" + allIFCRow.Count + Environment.NewLine;
            return allIFCRow;


        }


    }
}
