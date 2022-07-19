﻿using Ivan_Dal;
using System.Data;
using System.Web;

namespace Ivan_Service
{
    public class Dal_Sample_Chk_Dist : DataOperator
    {
        /// <summary>
        /// 樣品點收分配 查詢 Return DataTable
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public DataTable SearchTable(HttpContext context)
        {
			DataTable dt = new DataTable();
            string sqlStr = "";

			switch (context.Request["DATA_SOURCE"])
            {
                case "stkioh":
					sqlStr = @"SELECT  Top 500 '' 點收批號 
							   			      ,A.頤坊型號
							   			      ,S.產品說明
							   			      ,S.單位
							   			      ,A.出庫數 AS 可核銷數
							   			      ,ISNULL(S.內湖庫位,0) 內湖庫位
							   			      ,'' 到貨處理
							   			      ,A.廠商編號
							   			      ,A.廠商簡稱
							   			      ,A.暫時型號
							   			      ,'' 採購單號
							   			      ,A.序號
							   			      ,A.更新人員
							   			      ,CONVERT(VARCHAR,A.更新日期,23) 更新日期
                                              ,A.SUPLU_SEQ
							   FROM Stkioh A
							   INNER JOIN SUPLU S ON A.SUPLU_SEQ = S.序號
							   WHERE 1 = 1 ";
					break;
                case "suplu":
					sqlStr = @"SELECT  Top 500 '' 點收批號 
							   			      ,頤坊型號
							   			      ,A.產品說明
							   			      ,A.單位
							   			      ,0 AS 可核銷數
							   			      ,ISNULL(A.內湖庫位,0) 內湖庫位
							   			      ,'' 到貨處理
							   			      ,A.廠商編號
							   			      ,A.廠商簡稱
							   			      ,A.暫時型號
							   			      ,'' 採購單號
							   			      ,A.序號
							   			      ,A.更新人員
							   			      ,CONVERT(VARCHAR,A.更新日期,23) 更新日期
                                              ,序號 SUPLU_SEQ
							   FROM  SUPLU A
							   WHERE 1 = 1 ";
					break;
                default:
                    sqlStr = @"SELECT  Top 500 A.點收批號 
							   			      ,A.頤坊型號
							   			      ,S.產品說明
							   			      ,S.單位
							   			      ,ISNULL(A.點收數量,0) - ISNULL(A.核銷數量,0) AS 可核銷數
							   			      ,ISNULL(S.內湖庫位,0) 內湖庫位
							   			      ,P.到貨處理
							   			      ,A.廠商編號
							   			      ,A.廠商簡稱
							   			      ,A.暫時型號
							   			      ,A.採購單號
							   			      ,A.序號
							   			      ,A.更新人員
							   			      ,CONVERT(VARCHAR,A.更新日期,23) 更新日期
                                              ,P.SUPLU_SEQ
							   FROM recua A
							   INNER JOIN PUDU P ON A.PUDU_SEQ = P.序號
							   INNER JOIN SUPLU S ON P.SUPLU_SEQ = S.序號
							   WHERE ISNULL(A.點收數量,0) - ISNULL(A.核銷數量,0) > 0";
                    break;

            }

            //共用function 需調整日期名稱,form !=, 簡稱類, 串TABLE 簡稱 
            foreach (string form in context.Request.Form)
            {
                if (!string.IsNullOrEmpty(context.Request[form]) && form != "Call_Type" && form != "DATA_SOURCE")
                {
                    string debug = context.Request[form];
                    switch (form)
                    {
                        case "點收日期_S":
                            sqlStr += " AND CONVERT(DATE,A.[點收日期]) >= @點收日期_S";
                            this.SetParameters(form, context.Request[form]);
                            break;
                        case "點收日期_E":
                            sqlStr += " AND CONVERT(DATE,A.[點收日期]) <= @點收日期_E";
                            this.SetParameters(form, context.Request[form]);
                            break;
                        case "廠商簡稱":
                            sqlStr += " AND ISNULL(A.[廠商簡稱],'') LIKE '%' + @廠商簡稱 + '%'";
                            this.SetParameters(form, context.Request[form]);
                            break;
                        default:
                            sqlStr += " AND ISNULL(A.[" + form + "],'') LIKE @" + form + " + '%'";
                            this.SetParameters(form, context.Request[form]);
                            break;
                    }
                }
            }

            dt = GetDataTable(sqlStr);
			return dt;
        }

		/// <summary>
		/// 寫入出貨 TABLE 
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		public int InsertPaku2(HttpContext context)
		{
			int res = 0;
			string sqlStr = string.Format(@" INSERT INTO [dbo].[paku2]
														   ([序號]
														   ,[備貨日期]
														   ,[客戶編號]
														   ,[客戶簡稱]
														   ,[頤坊型號]
														   ,[暫時型號]
														   ,[產品說明]
														   ,[單位]
														   ,[備貨數量]
														   ,[核銷數量]
														   ,[廠商編號]
														   ,[廠商簡稱]
														   ,[來源]
														   ,[點收批號]
														   ,[已刪除]
														   ,[變更日期]
														   ,[更新人員]
														   ,[更新日期])
													SELECT (Select IsNull(Max(序號),0)+1 From [paku2]) [序號]
														   ,GETDATE() 備貨日期
														   ,@CUST_NO 客戶編號
														   ,@CUST_S_NAME 客戶簡稱
														   ,S.[頤坊型號]
														   ,S.[暫時型號]
														   ,S.[產品說明]
														   ,S.[單位]
														   ,@APP_CNT 備貨數量
														   ,NULL 核銷數量
														   ,S.[廠商編號]
														   ,S.[廠商簡稱]
														   ,@DATA_SOURCE 來源"
												+ (context.Request["DATA_SOURCE"] == "recua" ? ", 點收批號" : ", '' 點收批號")
												+ @"       ,NULL 已刪除
														   ,NULL 變更日期
														   ,@USER 更新人員
														   ,GETDATE() 更新日期
													  FROM {0} A", context.Request["DATA_SOURCE"]);

			if(context.Request["DATA_SOURCE"] == "recua")
            {
				sqlStr += @" INNER JOIN PUDU P ON A.PUDU_SEQ = P.序號 
							 INNER JOIN SUPLU S ON P.SUPLU_SEQ = S.序號 
							 WHERE A.序號 = @SEQ

							 UPDATE recua 
							 SET 核銷數量 = ISNULL(核銷數量,0) + @APP_CNT 
								,更新人員 = @USER
								,變更日期 = GETDATE()
							 WHERE 序號 = @SEQ ";
			}
			else if (context.Request["DATA_SOURCE"] == "stkioh")
            {
				sqlStr += @" JOIN suplu S ON A.SUPLU_SEQ = S.序號
							 WHERE A.序號 = @SEQ ";
			}
			else
            {
				sqlStr += " WHERE A.序號 = @SEQ ";
				sqlStr = sqlStr.Replace("S.","A.");
			}

			string[] seqArray = context.Request["SEQ[]"].Split(',');
			string[] appCntArray = context.Request["APP_CNT[]"].Split(',');
			for (int cnt = 0; cnt < seqArray.Length; cnt++)
			{
				this.ClearParameter();
				this.SetParameters("SEQ", seqArray[cnt]);
				this.SetParameters("DATA_SOURCE", context.Request["DATA_SOURCE"]);
				this.SetParameters("APP_CNT", appCntArray[cnt]);
				this.SetParameters("CUST_NO", context.Request["CUST_NO"]);
				this.SetParameters("CUST_S_NAME", context.Request["CUST_S_NAME"]);
				this.SetParameters("USER", "IVAN10");
				res = Execute(sqlStr);
			}
			return res;
		}
	}
}
