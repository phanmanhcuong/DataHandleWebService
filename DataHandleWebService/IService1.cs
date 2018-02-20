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

        [OperationContract]
        [WebInvoke(BodyStyle = WebMessageBodyStyle.Wrapped)]
        void StoreDataToDB(string imei, string strReceivedTime, string strLon,
            string strLat, string strSpeed, string strMNC, string strLAC, string strSOS,
            string strIsStrongboxOpen, string strIsEngineOn, string strIsStoping, string strIsGPSLost,
            string strTotalImageCam1, string strTotalImageCam2, string strRFID,
            string strOBD, string strVersion);
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
