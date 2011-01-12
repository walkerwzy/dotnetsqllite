using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBUtility.SQLite;
using System.Data;
using System.Xml;

namespace sqliteDemo
{
    public class db
    {
        public db()
        {

        }

        public static int add(model m)
        {
            int r = SQLiteHelper.ExecuteNonQuery("insert into user values(null,@uname,@udes,@title,@remark)", m.uname, m.udes, m.title, m.remark);
            return r;
        }

        public static DataSet getList(string strWhere)
        {
            //return SQLiteHelper.ExecuteDatasetWithTransaction("select * from user where uname=@uname or uname=@uname2", new string[] { "aaa", "bbb" });
            //return SQLiteHelper.ExecuteDataSet("select * from user where uname='aaa' or uname='ccc'", null);
            return SQLiteHelper.ExecuteDataSet("select * from user");
        }

        public static XmlReader getXml()
        {
            return SQLiteHelper.ExecuteXmlReader("select * from user where id not in (1)");
        }

        public static void delete(int id)
        {
            SQLiteHelper.ExecuteNonQuery("delete from user where id=@id", id);
        }
    }
}
