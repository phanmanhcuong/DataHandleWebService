using System;
using System.IO;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;

namespace DataHandleWebService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IService1" in both code and config file together.
    [ServiceContract]
    public interface IService1
    {

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/GetData", 
        BodyStyle = WebMessageBodyStyle.Wrapped)]
        void GetData(Stream stream);
    
        [OperationContract]
        CompositeType GetDataUsingDataContract(CompositeType composite);

        // TODO: Add your service operations here

        [OperationContract]
        void ExtractData(string data);

        //[OperationContract]
        //[WebInvoke(BodyStyle = WebMessageBodyStyle.Wrapped)]
        //void StoreDataToDBHistory(DataClassesDataContext db, int carID, int driverID,
        //    DateTime receivedTime, double longi, double lati, short gpsSpeed, int SPD, int cellId,
        //    int lacID, bool sos, bool isStrongboxOpen, bool isEngineOn, short carStatus, byte isGSPLost,
        //    string strRFID, int ENL, int COT, int RPM, int INTemp, int DIS, int MAF, double oriLati,
        //    double oriLongi, string cam1ImgPath, string cam2ImgPath);

        //[OperationContract]
        //[WebInvoke(BodyStyle = WebMessageBodyStyle.Wrapped)]
        //void StoreDataToDBOnlineRecord(DataClassesDataContext db, int carID, int driverID,
        //    DateTime receivedTime, double longi, double lati, short gpsSpeed, int SPD, int cellId,
        //    int lacID, bool sos, bool isStrongboxOpen, bool isEngineOn, short carStatus, byte isGPSLost,
        //    string strRFID, int ENL, int COT, int RPM, int INTemp, int DIS, int MAF, string strVersion);

        //[OperationContract]
        //[WebInvoke(BodyStyle = WebMessageBodyStyle.Wrapped)]
        //double DistanceGpsCalculate(double lat1, double lon1, double lat2, double lon2, char unit);

        //[OperationContract]
        //[WebInvoke(BodyStyle = WebMessageBodyStyle.Wrapped)]
        //void WriteDataToLogFile(string dataSplit, int imei, DateTime strReceivedTime, double strLon,
        //    double strLat, int strSpeed, int cellID, int lacID, bool sos,
        //    bool isStrongBoxOpen, bool isEngineOn, bool isStoping, byte isGPSLost,
        //    string strTotalImageCam1, string strTotalImageCam2, string strRFID,
        //    string strOBD, string strVersion);
    }


    // Use a data contract as illustrated in the sample below to add composite types to service operations.
    [DataContract]
    public class CompositeType
    {
        bool boolValue = true;
        string stringValue = "Hello ";

        [DataMember]
        public bool BoolValue
        {
            get { return boolValue; }
            set { boolValue = value; }
        }

        [DataMember]
        public string StringValue
        {
            get { return stringValue; }
            set { stringValue = value; }
        }
    }
}
