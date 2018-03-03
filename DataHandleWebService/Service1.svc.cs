using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace DataHandleWebService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select Service1.svc or Service1.svc.cs at the Solution Explorer and start debugging.
    public class Service1 : IService1
    {
        Dictionary<string, string> carHistory = new Dictionary<string, string>();
        const int MAX_DISTANCE = 50;
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
            System.Diagnostics.Debug.WriteLine(data);
            string[] dataLine = data.Split('?');
            string firstLine = dataLine[0];
            string[] cmd = dataLine[1].Split(',');
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
            
                using (DataClassesDataContext db = new DataClassesDataContext())
                {
                    DateTime currentDate = DateTime.UtcNow;
                    DateTime checkedTime = new DateTime(currentDate.Year, currentDate.Month, currentDate.Day, 0, 0, 0);

                    int carID = 0;
                    int driverID = 0;

                    carID = FindIdXeFromEmeiXe(imei);
                    driverID = FindIdLaiXeFromIdXe(carID);

                    if (!carHistory.ContainsKey(imei))
                    {
                        carHistory.Add(imei, strLat + "," + strLon);
                    }
                    else
                    {
                        carHistory[imei] = strLat + "," + strLon;
                    }

                    DateTime receivedTime = Convert.ToDateTime(strReceivedTime);
                    //chuyen sang gio VN
                    TimeSpan timeSpan = new TimeSpan(7, 0, 0);
                    receivedTime = receivedTime + timeSpan;

                    double longi = 0;
                    double lati = 0;

                    double oriLongi = 0;
                    double oriLati = 0;

                    double.TryParse(strLon, out longi);
                    double.TryParse(strLat, out lati);

                    short gpsSpeed = 0;
                    short.TryParse(strSpeed, out gpsSpeed);

                    int cellId = data.Contains("network") == true ? 1 : 0;
                    int lacID = int.Parse(strLAC, NumberStyles.HexNumber);

                    bool sos = false;
                    if (strSOS.Equals("1"))
                    {
                        sos = true;
                    }

                    bool isStrongboxOpen = false;
                    if (strIsStrongboxOpen.Equals("1"))
                    {
                        isStrongboxOpen = true;
                    }

                    bool isEngineOn = false;
                    if (strIsEngineOn.Equals("1"))
                    {
                        isEngineOn = true;
                    }

                    short carStatus = 0; //is runing
                    //kiem tra neu trang thai xe dang la chay nhung khoang cach vi tri den diem truoc do qua xa --> nhieu do ublox tinh sai vi tri
                    if (strIsStoping.Equals("0"))
                    {
                        if (carHistory.ContainsKey(imei))
                        {
                            string latlon = carHistory[imei];
                            string[] strTmp = latlon.Split(',');

                            double last_longi = 0;
                            double last_lati = 0;
                            double.TryParse(strTmp[1], out last_longi);
                            double.TryParse(strTmp[0], out last_lati);

                            if (DistanceGpsCalculate(lati, longi, last_lati, last_longi, 'K') > MAX_DISTANCE)
                            {
                                oriLati = lati;
                                oriLongi = longi;

                                lati = last_lati;
                                longi = last_longi;

                                carStatus = 2; //xe dang do nhung bi nhay vi tri
                            }
                        }
                    }
                    if (strIsStoping.Equals("1"))
                    {
                        carStatus = 1;               //dung
                        if (isEngineOn == false)
                        {
                            carStatus = 2;           //do
                        }
                    }

                    if (receivedTime.Hour > 21)
                    {
                        carStatus = 2;
                    }

                    byte isGPSLost = 0;
                    byte.TryParse(strIsGPSLost, out isGPSLost);

                    int SPD = 0;
                    int RPM = 0;
                    int DIS = 0;
                    int MAF = 0;

                    int ENL = 0, COT = 0, INTemp = 0;

                    string[] strOBD2 = strOBD.Split(';');
                    if (strOBD2.Length == 4)
                    {
                        int.TryParse(strOBD2[0].Trim(), out SPD);
                        int.TryParse(strOBD2[1].Trim(), out RPM);
                        int.TryParse(strOBD2[2].Trim(), out DIS);
                        int.TryParse(strOBD2[3].Trim(), out MAF);
                    }

                    if(carID > 0)
                    {
                        int newDis = 0;

                        if (longi >= 102 && longi <= 110) //ignore noise points outside of VietNam
                        {
                            //update or insert data into Lst_TrucTuyen
                            try
                            {
                                string strRFID2 = strRFID.Trim().Length > 255 ? strRFID.Trim().Substring(0, 255) : strRFID.Trim();
                                newDis = StoreDataToDBOnlineRecord(db, carID, driverID, receivedTime, longi, lati, gpsSpeed, SPD, cellId, lacID, sos, isStrongboxOpen, isEngineOn, carStatus, isGPSLost,
                                                strRFID2, ENL, COT, RPM, INTemp, DIS, MAF, strVersion);
                                string strRFID3 = strRFID.Trim().Length > 255 ? strRFID.Trim().Substring(0, 255) : strRFID.Trim();
                                //insert data into Log_LichSu
                                StoreDataToDBHistory(db, carID, driverID, receivedTime, longi, lati, gpsSpeed, SPD, cellId, lacID, sos, isStrongboxOpen, isEngineOn, carStatus, isGPSLost,
                                                    strRFID3, ENL, COT, RPM, INTemp, newDis, MAF, oriLati, oriLongi, strTotalImageCam1, strTotalImageCam1);
                                WriteDataToLogFile(firstLine, imei, receivedTime, longi, lati, gpsSpeed, cellId, lacID, sos, isStrongboxOpen, isEngineOn, strIsStoping, isGPSLost, strTotalImageCam1, strTotalImageCam2, strRFID2, SPD, RPM, DIS, MAF, strVersion);
                            }
                            catch(Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine(ex);
                            }
                        }
                    }
                }
            }
        }

        private void StoreDataToDBHistory(DataClassesDataContext db, int carID, int driverID, 
            DateTime receivedTime, double longi, double lati, short gpsSpeed, int SPD, int cellId, 
            int lacID, bool sos, bool isStrongboxOpen, bool isEngineOn, short carStatus, byte isGSPLost, 
            string strRFID, int ENL, int COT, int RPM, int INTemp, int DIS, int MAF, double oriLati, 
            double oriLongi, string cam1ImgPath, string cam2ImgPath)
        {
            history_record newRecord = new history_record();
            newRecord.id_xe = carID;
            newRecord.thoi_diem = receivedTime;
            newRecord.longi = longi;
            newRecord.lati = lati;
            newRecord.van_toc_gps = (byte)gpsSpeed;
            newRecord.van_toc_co = (byte)SPD;
            newRecord.cellid = cellId;
            newRecord.lac = lacID;

            newRecord.sos = sos;
            newRecord.mo_cua = isStrongboxOpen;
            newRecord.dong_co = isEngineOn;
            newRecord.trang_thai_xe = (byte)carStatus;
            newRecord.trang_thai_hopden = isGSPLost;

            newRecord.trang_thai_mo_rong = 0;

            newRecord.RFIDString = strRFID;

            newRecord.ENL = ENL;
            newRecord.COT = COT;
            newRecord.RPM = RPM;
            newRecord.INTemp = INTemp;
            newRecord.DIS = DIS;
            newRecord.PRESS = MAF;

            newRecord.timeout = 300;

            newRecord.id_lai_xe = driverID;
            newRecord.quang_duong_lien_tuc = oriLati; //dung tam truong quang_duong_lien tuc de luu toa do lati trong truong hop bij nhay diem
            newRecord.quang_duong_lien_tuc_gps = oriLongi;

            if (cam1ImgPath != null)
            {
                newRecord.cam1_img_path = cam1ImgPath;
            }
            if (cam2ImgPath != null)
            {
                newRecord.cam2_img_path = cam2ImgPath;
            }

            db.history_records.InsertOnSubmit(newRecord);
            db.SubmitChanges();
        }

        private int StoreDataToDBOnlineRecord(DataClassesDataContext db, int carID, int driverID, 
            DateTime receivedTime, double longi, double lati, short gpsSpeed, int SPD, int cellId, 
            int lacID, bool sos, bool isStrongboxOpen, bool isEngineOn, short carStatus, byte isGPSLost, 
            string strRFID, int ENL, int COT, int RPM, int INTemp, int DIS, int MAF, string strVersion)
        {
            int newDis = DIS;
            DateTime currentTimeUTC = DateTime.UtcNow;

            TimeZoneInfo cstZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            DateTime localTime = TimeZoneInfo.ConvertTimeFromUtc(currentTimeUTC, cstZone);

            var onlineRecord = (from o in db.online_records where o.id_xe == carID select o).FirstOrDefault();
            if(receivedTime.Year == 2000) //lost GPS
            {
                if(onlineRecord != null)
                {
                    onlineRecord.thoi_diem_nhan_tin = localTime;
                    onlineRecord.trang_thai_hopden = 1;
                    db.SubmitChanges();
                }
                return -1;
            }

            //check if carID is already in the online_record table
            if(onlineRecord != null)
            {
                TimeSpan diff = receivedTime.Subtract(onlineRecord.thoi_diem);

                if(diff.TotalSeconds > 0)
                {
                    onlineRecord.thoi_diem = receivedTime; //thoi diem nhan tin cua thiet bi gui len

                    onlineRecord.van_toc_gps = (byte)gpsSpeed; //van toc GPS
                    onlineRecord.van_toc_co = (byte)SPD; // van toc co hoc
                    onlineRecord.cellid = cellId; //ko có gps thì gửi cellid
                    onlineRecord.lac = lacID;     //cellID và LacID de ra vi tri theo network 

                    if (cellId == 1)
                    {
                        onlineRecord.cellid_gpslat = lati;
                        onlineRecord.cellid_gpslon = longi;
                    }
                    else
                    {
                        onlineRecord.longi = longi;
                        onlineRecord.lati = lati;
                    }

                    onlineRecord.sos = sos;  //neu nguoi dung bam nut SOS 
                    onlineRecord.mo_cua = isStrongboxOpen; //ket mo hay dong
                    onlineRecord.dong_co = isEngineOn;  // dong co bat hay tat
                    onlineRecord.trang_thai_xe = (byte)carStatus; //trang thai xe -: = 0 xe chay, = 1 xe dung, = 2 xe do
                    onlineRecord.trang_thai_hopden = isGPSLost;   //trang thai mat GPS
                    onlineRecord.trang_thai_mo_rong = 0;           //trang thai mo rong de dung sau nay     

                    onlineRecord.RFIDString = strRFID;            //chuoi chua cac gia tri RFID   

                    onlineRecord.ENL = ENL;                       //cac thong so doc duoc tu OBD de doc trang thai cua xe
                    onlineRecord.COT = COT;
                    onlineRecord.RPM = RPM;
                    onlineRecord.INTemp = INTemp;
                    if (DIS != 0)
                    {
                        onlineRecord.DIS = DIS;
                        newDis = DIS;
                    }
                    else
                    {
                        newDis = onlineRecord.DIS.Value;
                    }
                    onlineRecord.PRESS = MAF;

                    onlineRecord.timeout = 120; //2 phut   timeout --> quyet dinh xem xe da bi mat ket noi hay chua

                    onlineRecord.id_lai_xe = driverID;               // id cua driver neu co
                   // onlineRecord.id_chuhang = moneyOwnerID;          // chu hang - thu qui

                    onlineRecord.thoi_diem_nhan_tin = localTime;     // thoi diem nhan tin theo gio Server
                    onlineRecord.phienban = strVersion;              // phien ban cua firmware cua thiet bị 
                    onlineRecord.cellid_gpslat = lati;
                    onlineRecord.cellid_gpslon = longi;
                    db.SubmitChanges();
                }
            } else
            {
                try
                {
                    online_record newRecord = new online_record();

                    newRecord.id_xe = carID;
                    newRecord.thoi_diem = receivedTime;

                    newRecord.van_toc_gps = (byte)gpsSpeed;
                    newRecord.van_toc_co = (byte)SPD;
                    newRecord.cellid = cellId;
                    newRecord.lac = lacID;

                    if (cellId == 1)
                    {
                        newRecord.cellid_gpslat = lati;
                        newRecord.cellid_gpslon = longi;
                    }

                    newRecord.longi = longi;
                    newRecord.lati = lati;

                    newRecord.sos = sos;
                    newRecord.mo_cua = isStrongboxOpen;
                    newRecord.dong_co = isEngineOn;
                    newRecord.trang_thai_xe = (byte)carStatus;
                    newRecord.trang_thai_hopden = isGPSLost;
                    newRecord.trang_thai_mo_rong = 0;

                    newRecord.RFIDString = strRFID;

                    newRecord.ENL = ENL;
                    newRecord.COT = COT;
                    newRecord.RPM = RPM;
                    newRecord.INTemp = INTemp;
                    newRecord.DIS = DIS;
                    newRecord.PRESS = MAF;

                    newRecord.timeout = 120; //2 phut

                    newRecord.id_lai_xe = driverID;
                    //newRecord.id_chuhang = moneyOwnerID;đas

                    newRecord.thoi_diem_nhan_tin = localTime;
                    newRecord.phienban = strVersion;

                    db.online_records.InsertOnSubmit(newRecord);
                    db.SubmitChanges();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            }
            return newDis;
        }

        private double DistanceGpsCalculate(double lat1, double lon1, double lat2, double lon2, char unit)
        {
            try
            {
                double theta = lon1 - lon2;
                double dist = Math.Sin(deg2rad(lat1)) * Math.Sin(deg2rad(lat2)) +
                          Math.Cos(deg2rad(lat1)) * Math.Cos(deg2rad(lat2)) * Math.Cos(deg2rad(theta));
                dist = Math.Acos(dist);
                dist = rad2deg(dist);
                dist = dist * 60 * 1.1515;
                if (unit == 'K')
                {
                    dist = dist * 1.609344;
                }
                else if (unit == 'N')
                {
                    dist = dist * 0.8684;
                }
                if (double.IsNaN(dist)) return 0;
                return dist;
            }
            catch
            {
                return 0;
            }
        }
        //convert degree to radiant
        private double deg2rad(double deg)
        {
            return (deg * Math.PI / 180.0);
        }
        //convert radiant to degree
        private double rad2deg(double rad)
        {
            return (rad / Math.PI * 180.0);
        }

        private int FindIdXeFromEmeiXe(string imei)
        {
            using (DataClassesDataContext db = new DataClassesDataContext())
            {
                try
                {
                    var device = (from p in db.devices where p.imei == imei select p).FirstOrDefault();
                    if(device != null)
                    {
                        var car = (from c in db.Cars where c.id_thiet_bi == device.id_thiet_bi select c).FirstOrDefault();
                        return car.id_xe;

                    }
                } catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            }
            return 0;
        }

        private int FindIdLaiXeFromIdXe(int carId)
        {
            int driverID = 0; // no driver

            using (DataClassesDataContext db = new DataClassesDataContext())
            {
                try
                {
                    var car = (from c in db.Cars where c.id_xe == carId select c).FirstOrDefault();
                    if(car != null)
                    {
                        return (int)car.id_lai_xe_chinh;
                    }                   
                } catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            }
            return driverID;
        }


        private void WriteDataToLogFile(string firstLine, string imei, DateTime receivedTime, double longi, double lati, 
            short gpsSpeed, int cellId, int lacID, bool sos, bool isStrongboxOpen, bool isEngineOn, 
            string strIsStoping, byte isGPSLost, string strTotalImageCam1, string strTotalImageCam2, 
            string strRFID2, int SPD, int RPM, int DIS, int MAF, string strVersion)
        {
            string filePath = "C:\\Users\\Admin\\DevicesLogFilesReal\\" + imei + ".txt";
            if (File.Exists(filePath))
            {
                //delete file if file size > 20 Mb
                long size = new FileInfo(filePath).Length;
                if (size > 1048576)
                {
                    File.Delete(filePath);
                }
                else
                {
                    StreamWriter streamFile = new StreamWriter(filePath, append: true);
                    streamFile.WriteLine(firstLine);
                    string deviceInformation = imei + "," + Convert.ToString(receivedTime) + "," + longi +
                        "," + lati + "," + gpsSpeed + "," + cellId + "," + lacID + "," + sos +
                        "," + isStrongboxOpen + "," + isEngineOn + "," + strIsStoping + "," +
                        isGPSLost + "," + strTotalImageCam1 + "," + strTotalImageCam2 + "," + strRFID2 +
                        "," + SPD + ";" + RPM + ";" + DIS + ";" + MAF + "," + strVersion;
                    streamFile.WriteLine(deviceInformation);
                    streamFile.Close();
                }
            }
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
