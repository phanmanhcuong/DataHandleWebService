using System;
using System.IO;
using System.Linq;

namespace DataHandleWebService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select Service1.svc or Service1.svc.cs at the Solution Explorer and start debugging.
    public class Service1 : IService1
    {
        public void GetData(Stream data)
        {
            // convert Stream Data to StreamReader
            StreamReader reader = new StreamReader(data);
            // Read StreamReader data as string
            string postdata = reader.ReadToEnd();
            ExtractData(postdata);
            //System.Diagnostics.Debug.WriteLine(postdata);
        }

        public void ExtractData(string data)
        {
            string[] cmd = data.Split(',');
            if (cmd.Length >= 16)
            {
                string imei = cmd[0].Substring(1);
                string strReceivedTime = cmd[1];
                string strLon = cmd[2];
                string strLat = cmd[3];
                string strSpeed = cmd[4];
                string strMNC = cmd[5].Substring(1, cmd[5].Length - 2); //cellID: "32CB"
                string strLAC = cmd[6].Substring(1, cmd[6].Length - 2); //LacID: "32CB"

                string strSOS = cmd[7]; //co sos
                string strIsStrongboxOpen = cmd[8]; //co mo ket: dong/mo
                string strIsEngineOn = cmd[9]; //co dong co bat/tat
                string strIsStoping = cmd[10]; //co dung do dung/do
                string strIsGPSLost = cmd[11]; //co GPS mat/co
                string strTotalImageCam1 = cmd[12];
                string strTotalImageCam2 = cmd[13];
                string strRFID = cmd[14];
                string strOBD = cmd[15];

                string strVersion = cmd[cmd.Length - 2];
                StoreDataToDB(imei, strReceivedTime, strLon, strLat, strSpeed, strMNC, strLAC,
                    strSOS, strIsStrongboxOpen, strIsEngineOn, strIsStoping, strIsGPSLost, strTotalImageCam1,
                    strTotalImageCam2, strRFID, strOBD, strVersion);
            }
        }

        public void StoreDataToDB(string imei, string strReceivedTime, string strLon, string strLat, string strSpeed, string strMNC, string strLAC, string strSOS, string strIsStrongboxOpen, string strIsEngineOn, string strIsStoping, string strIsGPSLost, string strTotalImageCam1, string strTotalImageCam2, string strRFID, string strOBD, string strVersion)
        {
            DataClassesDataContext db = new DataClassesDataContext();
            history_record newRecord = new history_record();
            newRecord.imei = imei;
            newRecord.receivedTime = strReceivedTime;
            newRecord.longitude = strLon;
            newRecord.latitude = strLat;
            newRecord.speed = strSpeed;
            newRecord.mnc = strMNC; //cellID: "32CB"
            newRecord.lac = strLAC; //LacID: "32CB"

            newRecord.sos = strSOS; //co sos
            newRecord.strong_box_open = strIsStrongboxOpen; //co mo ket: dong/mo
            newRecord.engine_on = strIsEngineOn; //co dong co bat/tat
            newRecord.stopping = strIsStoping; //co dung do dung/do
            newRecord.gps_lost = strIsGPSLost; //co GPS mat/co
            if(strTotalImageCam1 != null)
            {
                newRecord.image_cam1 = strTotalImageCam1;
            }
            if (strTotalImageCam1 != null)
            {
                newRecord.image_cam2 = strTotalImageCam2;
            }
            newRecord.rfid = strRFID;
            newRecord.obd = strOBD;
            newRecord.version = strVersion;

            db.history_records.InsertOnSubmit(newRecord);
            db.SubmitChanges();      
        }

        public CompositeType GetDataUsingDataContract(CompositeType composite)
        {
            if (composite == null)
            {
                throw new ArgumentNullException("composite");
            }
            if (composite.BoolValue)
            {
                composite.StringValue += "Suffix";
            }
            return composite;
        }

    }
}
