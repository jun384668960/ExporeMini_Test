using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using vlc.net;

namespace ExporeMini_Test
{
    public partial class ExporeMini_Test : Form
    {
        public enum CMD_TYPE_E
        {
            CMD_START_RTP = 0xe1<<8 | 0x01,
            CMD_STOP_RTP = 0xe1 << 8 | 0x02,
            CMD_GET_SPS_PPS = 0xe1 << 8 | 0x03,
            CMD_GET_CAMERA_BASIC_PARAM = 0xe1 << 8 | 0x04,
            CMD_SET_PREVIEW_RESOLUTION = 0xe1 << 8 | 0x05,

            CMD_START_RECORD = 0xe1 << 8 | 0x06,
            CMD_STOP_RECORD = 0xe1 << 8 | 0x07,
            CMD_GET_RECORD_VIDEO_TIME = 0xe1 << 8 | 0x08,
            CMD_TAKE_PHOTO = 0xe1 << 8 | 0x09,
            CMD_GET_RECORD_VIDEO_FREE_TIME = 0xe1 << 8 | 0x0a,
            CMD_GET_FREE_SPACE = 0xe1 << 8 | 0x0b,
            CMD_GET_SAVE_PATH = 0xe1 << 8 | 0x0c,
            CMD_SET_SAVE_PATH = 0xe1 << 8 | 0x0d,
            CMD_CLEAR_SAVE_PATH = 0xe1 << 8 | 0x0e,
            CMD_ROM_STATUS = 0xe1 << 8 | 0x0f,

            CMD_CAMERA_PARAM_DETAIL = 0xe1 << 8 | 0x10,
            CMD_CAMERA_DESC_PARAM_DETAIL = 0xe1 << 8 | 0x11,

            CMD_GET_UAV_TIME = 0xe0 << 8 | 0xc1,
            CMD_SET_UAV_TIME = 0xe0 << 8 | 0xc2,
            CMD_MOD_WIFI_SSID_PASSWD = 0xe0 << 8 | 0xc3,
            CMD_GET_UAV_PARAM = 0xe0 << 8 | 0xc4,
            CMD_PERMISSION_STATUS = 0xe0 << 8 | 0xc5,

            CMD_CONTROL_UNLOCK = 0x51 <<8 | 0x01,
            CMD_CONTROL_LOCK = 0x51 << 8| 0x02,
            CMD_CONTROL_TAKEING_OFF = 0x51 << 8 | 0x03,
            CMD_CONTROL_TAKEING_LOADING = 0x51 << 8 | 0x04,
            CMD_CONTROL_TAKEING_HOVER = 0x51 << 8 | 0x05,
            CMD_CONTROL_TAKEING_RETURN_BACK = 0x51 << 8 | 0x06,

            CMD_START_CHECK_COMPASS = 0x97 << 8 | 0x01,
            CMD_START_IMAGE_FOLLOW = 0x97 << 8 | 0x08,
            CMD_STOP_IMAGE_FOLLOW = 0x97 << 8 | 0x09,
            CMD_MAX
        };
        public struct CMD_PACKET_HEAD_S
        {
            public byte startBit;
            public byte length;
            public short cmdId;
            public short seq;
        };
        public struct CMD_PACKET_S
        {
            public CMD_PACKET_HEAD_S head;
            public byte[] exData;
            public byte mask;
        }
    
        private byte[] req_buff = new byte[256];
        private byte[] resp_buff = new byte[256];
        NetworkStream streamToServer;

        private CMD_TYPE_E g_CmdType = CMD_TYPE_E.CMD_START_RTP;

        //VLC PLAYER
        private VlcPlayer vlc_player_;
        private bool is_playinig_;
        private bool is_start_;
        private short g_CurSeq = 0;

        //CAMERA PARAMS
        private byte clear_save_path_var = 0;
        private byte preview_resolution = 0;
        private byte video_resolution = 2;
        private byte picture_size = 2;
        private byte take_mode = 2;
        private int resolution_type = 0;
        private byte camera_param_detail_type = 0;
        private byte camera_set_awb_type = 0;
        private byte camera_set_iso_type = 0;
        private byte camera_set_ev_type = 0;
        private byte camera_set_delay_time = 3;
        private byte camera_set_save_path_type = 0;
        private byte camera_set_take_photo_count = 0;
        private int compass_status = -1;
        // NET CONNECTION.
        UdpClient udpClient;
        private int udpInitFlag = 0;
        private string udpServer = "127.0.0.1";
        private int updPort =60000;
        private string vlc_url = "rtsp://192.168.43.1:8553/videoCodecType=H.264";
        private int vlc_buff_time = 400;
        IPEndPoint remoteIpep;
        //WIFI SETTING
        private string wifiSsid;
        private string wifiPasswd;
        private Thread startServer;

        private void udpReceiveThread()
        {
            while(true)
            {
                 try{

                     if (udpInitFlag == 0)
                     {
                         Thread.Sleep(1);
                         continue;

                     }

                   resp_buff = udpClient.Receive(ref remoteIpep);
                   HandleRespDetil(resp_buff, resp_buff.Length);
                 }
                 catch (Exception e)
                 {
                     Console.WriteLine(e.ToString());
                     MessageBox.Show("udpReceiveThread failed.@" + e.ToString());
                 }  
            };
            
        }

        public void init_udpClient()
        {
           
            try{

                if (udpInitFlag == 1)
                {
                   //TODO
                
                }

               
                remoteIpep = new IPEndPoint(IPAddress.Parse(udpServer), updPort); 
                udpClient = new UdpClient();               
                udpClient.Connect(udpServer, updPort);
                                
                //Byte[] sendBytes = Encoding.ASCII.GetBytes("udp client is coming.");
                //udpClient.Send(sendBytes, sendBytes.Length);

                udpInitFlag = 1;
             }  
             catch (Exception e ) {
                  Console.WriteLine(e.ToString());
                  MessageBox.Show("udpclient connect failed.@"+e.ToString());
             }        
        }

        public ExporeMini_Test()
        {
            InitializeComponent();

            string pluginPath = System.Environment.CurrentDirectory + "\\vlc\\plugins\\";
            //MessageBox.Show(pluginPath);
            vlc_player_ = new VlcPlayer(pluginPath);
            IntPtr render_wnd = this.panel1.Handle;
            vlc_player_.SetRenderWindow((int)render_wnd);
            
            is_playinig_ = false;

            startServer = new Thread(new ThreadStart(udpReceiveThread));
            startServer.Start();
            
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            planetSettingFormshow();
        }

        private void sendCmdToPlanet()
        {
 
            //打印16进制           
            int length;
            string resp_s = "";
            req_buff = BuildCmdReq(g_CmdType, out length);
            for (int i = 0; i < length; i++)
            {
                resp_s += "0x" + req_buff[i].ToString("x2") + " ";
            }

            rtxb_cmdDetil.Text = resp_s;
            rtxb_resp.Text = "";
            textBox1.Text = "";
            udpClient.Send(req_buff, length);           
        }

        private byte[] BuildCmdReq(CMD_TYPE_E cmd, out int length)
        {
            int len = 0;
            byte[] buff = new byte[256];
            
            switch (cmd)
            {

                case CMD_TYPE_E.CMD_START_IMAGE_FOLLOW:
                    {
                        //TODO: 开始视频跟随
                        len = 17;
                    }
                    break;
                case CMD_TYPE_E.CMD_CAMERA_PARAM_DETAIL:
                    {
                        buff[6] = camera_param_detail_type;
                        if (camera_param_detail_type == 0)
                        {                            
                            buff[7] = camera_set_awb_type;                            
                        }
                        else if (camera_param_detail_type == 1)
                        {                            
                            buff[7] = camera_set_iso_type;                         
                        }
                        else if (camera_param_detail_type == 2)
                        {                            
                            buff[7] = camera_set_ev_type;                         
                        } else if (camera_param_detail_type == 3)
                        {                            
                            buff[7] = camera_set_take_photo_count;

                        }
                        else if (camera_param_detail_type == 4)
                        {
                            buff[7] = camera_set_delay_time;

                        }
                        len = 2;
                    }
                    break;
                case CMD_TYPE_E.CMD_SET_PREVIEW_RESOLUTION:
                    {
                        if (resolution_type == 0)
                        {
                            buff[6] = 0;
                            buff[7] = preview_resolution;
                        }
                        else if (resolution_type == 2)
                        {
                            buff[6] = 2;
                            buff[7] = picture_size;
                        }
                        else if (resolution_type == 3)
                        {
                            buff[6] = 3;
                            buff[7] = take_mode;
                        }
                        else if (resolution_type == 1)
                        {
                            buff[6] = 1;
                            buff[7] = video_resolution;

                        }
                        len = 2;
                    }
                    break;

                case CMD_TYPE_E.CMD_MOD_WIFI_SSID_PASSWD:
                    {
                
                        System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
                        Byte[] wifiSsidbytes = encoding.GetBytes(wifiSsid);
                        Byte[] wifiPasswdbytes = encoding.GetBytes(wifiPasswd);
                        buff[6] = (byte)wifiSsidbytes.Length;
                        int start = 7;
                        for(int i=0; i< wifiSsidbytes.Length; i++)
                        {
                            buff[start] = wifiSsidbytes[i];
                            start++;
                        }
                        start = 6 + 1 + (byte)wifiSsidbytes.Length;
                        buff[start] = (byte)wifiPasswdbytes.Length;
                        for (int i = 0; i < wifiPasswdbytes.Length; i++)
                        {
                            start++;
                            buff[start] = wifiPasswdbytes[i];                            
                        }
                        len = 2 + (byte)wifiPasswdbytes.Length + (byte)wifiSsidbytes.Length;
                    }
                    break;
                case CMD_TYPE_E.CMD_STOP_IMAGE_FOLLOW:
                case CMD_TYPE_E.CMD_START_CHECK_COMPASS:
                case CMD_TYPE_E.CMD_GET_SAVE_PATH:
                case CMD_TYPE_E.CMD_CAMERA_DESC_PARAM_DETAIL:
                case CMD_TYPE_E.CMD_GET_CAMERA_BASIC_PARAM:
                case CMD_TYPE_E.CMD_GET_UAV_PARAM:
                case CMD_TYPE_E.CMD_GET_UAV_TIME:
                case CMD_TYPE_E.CMD_GET_SPS_PPS:
                case CMD_TYPE_E.CMD_GET_RECORD_VIDEO_TIME:
                case CMD_TYPE_E.CMD_GET_RECORD_VIDEO_FREE_TIME:
                case CMD_TYPE_E.CMD_GET_FREE_SPACE:
                    {
                        len = 0;
                    }
                    break;

                case CMD_TYPE_E.CMD_SET_SAVE_PATH:
                    {
                        buff[6] = camera_set_save_path_type;
                        len = 1;
                    }
                    break;
                case CMD_TYPE_E.CMD_CLEAR_SAVE_PATH:
                    {
                        buff[6] = clear_save_path_var;
                        len = 1;
                    }
                    break;
                case CMD_TYPE_E.CMD_SET_UAV_TIME:
                    {
                        buff[6] = 0;
                        buff[7] = 0;
                        buff[8] = 0;
                        buff[9] = 0;
                        buff[10] = 0;
                        buff[11] = 0;
                        buff[12] = 0;
                        buff[13] = 0;
                        len = 8;
                    }
                    break;
                case CMD_TYPE_E.CMD_CONTROL_UNLOCK:
                case CMD_TYPE_E.CMD_CONTROL_LOCK:
                case CMD_TYPE_E.CMD_CONTROL_TAKEING_OFF:
                case CMD_TYPE_E.CMD_CONTROL_TAKEING_LOADING:
                case CMD_TYPE_E.CMD_CONTROL_TAKEING_HOVER:
                case CMD_TYPE_E.CMD_CONTROL_TAKEING_RETURN_BACK:
                    {
                        
                        byte tmp = (byte)cmd ;
                        tmp += 0xA0;
                        buff[6] = tmp;
                        buff[7] = tmp;
                        buff[8] = tmp;
                        buff[9] = tmp;
                        len = 4;
                    }
                    break;

                case CMD_TYPE_E.CMD_START_RTP:
                    {
                        //端口号
                        buff[6] = 0x22;
                        buff[7] = 0x01;

                        len = 2;
                    }
                    break;
                case CMD_TYPE_E.CMD_STOP_RTP:
                    {
                        len = 0;
                    }
                    break;
                case CMD_TYPE_E.CMD_TAKE_PHOTO:
                case CMD_TYPE_E.CMD_START_RECORD:
                case CMD_TYPE_E.CMD_STOP_RECORD:
                    {
                        buff[6] = 0x00;
                        len = 1;
                    }
                    break;
                default:
                    len = -1;
                    break;
            }

            if (len != -1)
            {
                //起始位
                buff[0] = 0xA0;

                //长度
                buff[1] = (byte)(6 + len + 1);  //head + data + mask

                UInt16 temp_s = 0x0000;
                byte[] temp;

                //序列号
                g_CurSeq++;
                temp_s = (UInt16)g_CurSeq;
                temp = BitConverter.GetBytes(temp_s);
                Array.Reverse(temp);
                buff[4] = temp[0];
                buff[5] = temp[1];
                //buff[4] = (byte)(g_CurSeq >> 8 & 0xff);
                //buff[5] = (byte)(g_CurSeq & 0xff);

                //cmd id
                temp_s = (UInt16)cmd;
                temp = BitConverter.GetBytes(temp_s);
                Array.Reverse(temp);
                buff[2] = temp[0];
                buff[3] = temp[1];

                //mask
                length = buff[1];
                buff[length - 1] = Utils.Xiro_buildCheckBit(buff, length - 1);
            }
            else 
            {
                length = -1;
            }
            return buff;
        }

        private void HandleRespDetil(byte[] resp, int length)
        {
            
            if (length > 0)
            {
                string resp_msg = "";
                do
                {
                    //如果跟当前g_CurSeq相同
                    short resp_seq = (short)(resp[4] << 8 | resp[5]);
                    //if (resp_seq == g_CurSeq)
                    {
                        //maks验证
                        bool r = Utils.Xiro_CheckBitEn(resp, length);
                        if (!r)
                        {                           
                            break;
                        }

                        if (resp[2] == 5)
                        { //高频单向下行命令 开始指南针效验 –> 开始主推 (1hz) 直到成功或者失败 最后再发送5次 停止主推
                            if (length >= 5)
                            {
                                if (compass_status != resp[3])
                                {
                                    compass_status = resp[3];
                                    if (compass_status == 0) MessageBox.Show("指南针效验--开始");
                                    else if (compass_status == 1) MessageBox.Show("指南针效验--成功");
                                    else if (compass_status == 2) MessageBox.Show("指南针效验--失败");
                                    else if (compass_status == 3) MessageBox.Show("指南针效验--水平");
                                    else if (compass_status == 4) MessageBox.Show("指南针效验--垂直");
                                }
                            }
                        }
                        else if (resp[2] == 0x48) 
                        { 
                            
                        }
                        else if (resp[2] == 0x97 && resp[3] == 0x10)
                        { //视频跟随回调

                        }
                        //解析cmd
                        int resp_len = (int)resp[1];
                        CMD_PACKET_S pack = new CMD_PACKET_S();
                        pack.head = Utils.BytesToStruct<CMD_PACKET_HEAD_S>(resp);

                        //数据
                        int exDataLen = resp_len - 7;
                        pack.exData = new byte[exDataLen];
                        Array.Copy(resp, Marshal.SizeOf(pack.head), pack.exData, 0, exDataLen);

                        //解析是否成功及其参数
                        bool success = true;
                      
                        UInt16 _cmdId = (UInt16)IPAddress.NetworkToHostOrder(pack.head.cmdId);
                        CMD_TYPE_E type = (CMD_TYPE_E)_cmdId;
                        
                        switch (type)
                        {
                            case CMD_TYPE_E.CMD_START_RTP:
                                {
                                    //解析   
                                    string cmdstr = "开始发送rtp包";
                                    success = pack.exData[0] == 0x01 ? true : false;
                                    if (success)
                                    {
                                        resp_msg = cmdstr + "成功";
                                        is_start_ = true;
                                    }
                                    else
                                    {
                                        resp_msg = cmdstr + "失败@错误码: " + pack.exData[1];
                                    }
                                }
                                break;
                            case CMD_TYPE_E.CMD_STOP_RTP:
                                {
                                    string cmdstr = "停止发送rtp包";
                                    success = pack.exData[0] == 0x01 ? true : false;
                                    if (success)
                                    {
                                        resp_msg = cmdstr + "成功";
                                        is_start_ = false;
                                    }
                                    else
                                    {
                                        resp_msg = cmdstr + "失败@错误码: " + pack.exData[1];
                                    }
                                }
                                break;
                            case CMD_TYPE_E.CMD_TAKE_PHOTO:
                                {
                                    success = pack.exData[1] == 0x01 ? true : false;
                                    if (success)
                                    {
                                        resp_msg = "执照成功";
                                    }
                                    else
                                        resp_msg = "执照失败@错误码: " + pack.exData[2];
                                }
                                break;
                            case CMD_TYPE_E.CMD_START_RECORD:
                                {
                                    string cmdstr = "开始录相";
                                    success = pack.exData[1] == 0x01 ? true : false;
                                    if (success)
                                    {
                                        resp_msg = cmdstr + "成功";
                                    }
                                    else
                                    {
                                        resp_msg = cmdstr + "失败@错误码: " + pack.exData[2];
                                    }
                                }
                                break;
                            case CMD_TYPE_E.CMD_STOP_RECORD:
                                {
                                    string cmdstr = "停止录相";
                                    success = pack.exData[1] == 0x01 ? true : false;
                                    if (success)
                                    {
                                        resp_msg = cmdstr + "成功";
                                    }
                                    else
                                    {
                                        resp_msg = cmdstr + "失败@错误码: " + pack.exData[2];
                                    }
                                }
                                break;


                            case CMD_TYPE_E.CMD_CONTROL_UNLOCK:
                                {
                                    success = pack.exData[0] == 0x01 ? true : false;
                                    if (success)
                                    {
                                        resp_msg = "一键解锁" + "成功";
                                    }
                                    else {
                                        resp_msg = "一键解锁" + "失败@错误码: " + pack.exData[1];
                                    }
                                }
                                break;
                            case CMD_TYPE_E.CMD_CONTROL_LOCK:
                                {
                                    success = pack.exData[0] == 0x01 ? true : false;
                                    if (success)
                                    {
                                        resp_msg = "一键加锁" + "成功";
                                    }
                                    else
                                    {
                                        resp_msg = "一键加锁" + "失败@错误码: " + pack.exData[1];
                                    }
                                }
                                break;
                            case CMD_TYPE_E.CMD_CONTROL_TAKEING_OFF:
                                {
                                    success = pack.exData[0] == 0x01 ? true : false;
                                    if (success)
                                    {
                                        resp_msg = "一键起飞" + "成功";
                                    }
                                    else
                                    {
                                        resp_msg = "一键起飞" + "失败@错误码: " + pack.exData[1];
                                    }
                                }
                                break;
                            case CMD_TYPE_E.CMD_CONTROL_TAKEING_LOADING:
                                {
                                    string cmdstr = "一键降落";
                                    success = pack.exData[0] == 0x01 ? true : false;
                                    if (success)
                                    {
                                        resp_msg = cmdstr + "成功";
                                    }
                                    else
                                    {
                                        resp_msg = cmdstr + "失败@错误码: " + pack.exData[1];
                                    }
                                }
                                break;
                           
                            case CMD_TYPE_E.CMD_START_IMAGE_FOLLOW:
                                {
                                    string cmdstr = "开始视频跟随";
                                    success = pack.exData[0] == 0x01 ? true : false;
                                    if (success)
                                    {
                                        resp_msg = cmdstr + "成功";
                                    }
                                    else
                                    {
                                        resp_msg = cmdstr + "失败@错误码: " + pack.exData[1];
                                    }
                                }
                                break;
                            case CMD_TYPE_E.CMD_STOP_IMAGE_FOLLOW:
                                {
                                    string cmdstr = "停止视频跟随";
                                    success = pack.exData[0] == 0x01 ? true : false;
                                    if (success)
                                    {
                                        resp_msg = cmdstr + "成功";
                                    }
                                    else
                                    {
                                        resp_msg = cmdstr + "失败@错误码: " + pack.exData[1];
                                    }
                                }
                                break;
                            case CMD_TYPE_E.CMD_START_CHECK_COMPASS:
                                {
                                    string cmdstr = "开始指南针效验";
                                    success = pack.exData[0] == 0x01 ? true : false;
                                    if (success)
                                    {
                                        resp_msg = cmdstr + "成功";
                                    }
                                    else
                                    {
                                        resp_msg = cmdstr + "失败@错误码: " + pack.exData[1];
                                    }
                                }
                                break;
                            case CMD_TYPE_E.CMD_GET_SAVE_PATH:
                                {
                                    string cmdstr = "查询存储路径";
                                    success = pack.exData[0] == 0x01 ? true : false;
                                    if (success)
                                    {
                                        resp_msg = cmdstr + "成功";
                                    }
                                    else
                                    {
                                        resp_msg = cmdstr + "失败@错误码: " + pack.exData[1];
                                    }
                                }
                                break;
                            case CMD_TYPE_E.CMD_SET_SAVE_PATH:
                                {
                                    string cmdstr = "设置存储路径";
                                    success = pack.exData[0] == 0x01 ? true : false;
                                    if (success)
                                    {
                                        resp_msg = cmdstr + "成功";
                                    }
                                    else
                                    {
                                        resp_msg = cmdstr + "失败@错误码: " + pack.exData[1];
                                    }
                                }
                                break;
                            case CMD_TYPE_E.CMD_CONTROL_TAKEING_HOVER:
                                {
                                    string cmdstr = "一键悬停";
                                    success = pack.exData[0] == 0x01 ? true : false;
                                    if (success)
                                    {
                                        resp_msg = cmdstr + "成功";
                                    }
                                    else
                                    {
                                        resp_msg = cmdstr + "失败@错误码: " + pack.exData[1];
                                    }
                                }
                                break;
                            case CMD_TYPE_E.CMD_SET_PREVIEW_RESOLUTION:
                                {
                                    string cmdstr = "设置Camera基础参数";
                                    success = pack.exData[0] == 0x01 ? true : false;
                                    if (success)
                                    {
                                        resp_msg = cmdstr + "成功";
                                    }
                                    else
                                    {
                                        resp_msg = cmdstr + "失败@错误码: " + pack.exData[1];
                                    }
                                }
                                break;                  
                            case CMD_TYPE_E.CMD_GET_CAMERA_BASIC_PARAM:
                                {
                                    string cmdstr = "获取Camera基础参数";
                                    success = pack.exData[0] == 0x01 ? true : false;
                                    if (success)
                                    {
                                       

                                        resp_msg = cmdstr + "成功, FPV预览分辨率:";
                                        if (pack.exData[1] == 0) resp_msg += "VGA_30 ";
                                        else if (pack.exData[1] == 1) resp_msg += "720P_30";
                                        else if (pack.exData[1] == 2) resp_msg += "320X240 ";

                                        resp_msg += "；录像分辨率:";
                                        if (pack.exData[2] == 1) resp_msg += "720p30 ";
                                        else if (pack.exData[2] == 2) resp_msg += "1080p30";
                                        else if (pack.exData[2] == 3) resp_msg += "1080p60 ";
                                        else if (pack.exData[2] == 4) resp_msg += "4k30 ";

                                        resp_msg += "；拍照分辨率:";
                                        if (pack.exData[3] == 0) resp_msg += "8M_16_9 ";
                                        else if (pack.exData[3] == 1) resp_msg += "13M_4_3";
                                        else if (pack.exData[3] == 2) resp_msg += "9M_1_1_ ";

                                        resp_msg += "；拍照模式:";
                                        if (pack.exData[4] == 0) resp_msg += "单拍  ";
                                        else if (pack.exData[4] == 1) resp_msg += "连拍 ";
                                        else if (pack.exData[4] == 2) resp_msg += "延时拍照 ";
                                    }
                                    else
                                    {
                                        resp_msg = cmdstr + "失败@错误码: " + pack.exData[1];
                                    }
                                }
                                break;

                            case CMD_TYPE_E.CMD_CAMERA_DESC_PARAM_DETAIL:
                                {
                                    string cmdstr = "获取Camera详细参数";
                                    success = pack.exData[0] == 0x01 ? true : false;
                                    if (success)
                                    {


                                        resp_msg = cmdstr + "成功, AWB:";
                                        if (pack.exData[1] == 0) resp_msg += "auto  ";
                                        else if (pack.exData[1] == 1) resp_msg += "incandescent ";
                                        else if (pack.exData[1] == 2) resp_msg += "fluorescent  ";
                                        else if (pack.exData[1] == 3) resp_msg += "warm_fluorescent  ";
                                        else if (pack.exData[1] == 4) resp_msg += "daylight  ";
                                        else if (pack.exData[1] == 5) resp_msg += "cloudy_daylight  ";
                                        else if (pack.exData[1] == 6) resp_msg += "twilight  ";
                                        else if (pack.exData[1] == 7) resp_msg += "shade  ";
                                        resp_msg += "；ISO:";

                                        if (pack.exData[2] == 0) resp_msg += "auto ";
                                        else if (pack.exData[2] == 1) resp_msg += "100";
                                        else if (pack.exData[2] == 2) resp_msg += "200";
                                        else if (pack.exData[2] == 3) resp_msg += "400 ";
                                        else if (pack.exData[2] == 4) resp_msg += "800 ";
                                        else if (pack.exData[2] == 5) resp_msg += "1600  ";
                                        else if (pack.exData[2] == 6) resp_msg += "3200  ";
                                        

                                        resp_msg += "；EV:";
                                        if (pack.exData[3] == 0) resp_msg += "0 ";
                                        else if (pack.exData[3] == 1) resp_msg += "-6";
                                        else if (pack.exData[3] == 2) resp_msg += "-4";
                                        else if (pack.exData[3] == 3) resp_msg += "-2 ";
                                        else if (pack.exData[3] == 4) resp_msg += "2 ";
                                        else if (pack.exData[3] == 5) resp_msg += "4  ";
                                        else if (pack.exData[3] == 6) resp_msg += "6  ";

                                        resp_msg += "；连拍张数:" + pack.exData[4];

                                        resp_msg += "；延时拍照时间 :" + pack.exData[5];
                                    }
                                    else
                                    {
                                        resp_msg = cmdstr + "失败@错误码: " + pack.exData[1];
                                    }
                                }
                                break;      
                            case CMD_TYPE_E.CMD_CONTROL_TAKEING_RETURN_BACK:
                                {
                                    string cmdstr = "一键返航";
                                    success = pack.exData[0] == 0x01 ? true : false;
                                    if (success)
                                    {
                                        resp_msg = cmdstr + "成功";
                                    }
                                    else
                                    {
                                        resp_msg = cmdstr + "失败@错误码: " + pack.exData[1];
                                    }
                                }
                                break;
                            case CMD_TYPE_E.CMD_CAMERA_PARAM_DETAIL:
                                {
                                    string cmdstr = "配置相机详细参数";
                                    success = pack.exData[0] == 0x01 ? true : false;
                                    if (success)
                                    {
                                        resp_msg = cmdstr + "成功";
                                    }
                                    else
                                    {
                                        resp_msg = cmdstr + "失败@错误码: " + pack.exData[1];
                                    }
                                }
                                break;
                            case CMD_TYPE_E.CMD_GET_SPS_PPS:
                                {
                                    string cmdstr = "获取sps pps 数据";
                                    success = pack.exData[0] == 0x01 ? true : false;
                                    if (success)
                                    {
                                        resp_msg = cmdstr + "成功";
                                    }
                                    else
                                    {
                                        resp_msg = cmdstr + "失败@错误码: " + pack.exData[1];
                                    }
                                }
                                break;
                            case CMD_TYPE_E.CMD_GET_RECORD_VIDEO_TIME:
                                {
                                    string cmdstr = "获取录像时间";
                                    success = pack.exData[0] == 0x01 ? true : false;
                                    if (success)
                                    {
                                        uint time = 0;
                                        time = pack.exData[4];
                                        time = (time << 8) + pack.exData[3];
                                        time = (time << 8) + pack.exData[2];
                                        time = (time << 8) + pack.exData[1];
                                        resp_msg = cmdstr + "成功,录像时间:" + time;
                                    }
                                    else
                                    {
                                        resp_msg = cmdstr + "失败@错误码: " + pack.exData[1];
                                    }
                                }
                                break;
                            case CMD_TYPE_E.CMD_GET_RECORD_VIDEO_FREE_TIME:
                                {
                                    string cmdstr = "获取存储空间剩余录像时间";
                                    uint time = 0;
                                    time = pack.exData[4];
                                    time = (time << 8) + pack.exData[3];
                                    time = (time << 8) + pack.exData[2];
                                    time = (time << 8) + pack.exData[1];
                                    success = pack.exData[0] == 0x01 ? true : false;
                                    if (success)
                                    {
                                        resp_msg = cmdstr + "成功,录像时间:" + time;
                                    }
                                    else
                                    {
                                        resp_msg = cmdstr + "失败@错误码: " + pack.exData[1];
                                    }
                                }
                                break;
                            case CMD_TYPE_E.CMD_GET_FREE_SPACE:
                                {
                                    string cmdstr = "获取剩余容量";
                                    uint space = 0;
                                    space = pack.exData[4];
                                    space = (space << 8) + pack.exData[3];
                                    space = (space << 8) + pack.exData[2];
                                    space = (space << 8) + pack.exData[1];
                                    success = pack.exData[0] == 0x01 ? true : false;
                                    if (success)
                                    {
                                        resp_msg = cmdstr + "成功,录像时间:" + space + "KB";
                                    }
                                    else
                                    {
                                        resp_msg = cmdstr + "失败@错误码: " + pack.exData[1];
                                    }
                                }
                                break;
                            case CMD_TYPE_E.CMD_CLEAR_SAVE_PATH:
                                {
                                    string cmdstr = "清空存储目录";
                                    success = pack.exData[0] == 0x01 ? true : false;
                                    if (success)
                                    {
                                        resp_msg = cmdstr + "成功";
                                    }
                                    else
                                    {
                                        resp_msg = cmdstr + "失败@错误码: " + pack.exData[1];
                                    }
                                }
                                break;
                            case CMD_TYPE_E.CMD_GET_UAV_TIME:
                                {
                                    string cmdstr = "查询无人机时间";
                                    success = pack.exData[0] == 0x01 ? true : false;
                                    if (success)
                                    {
                                        resp_msg = cmdstr + "成功";
                                    }
                                    else
                                    {
                                        resp_msg = cmdstr + "失败@错误码: " + pack.exData[1];
                                    }
                                }
                                break;
                            case CMD_TYPE_E.CMD_SET_UAV_TIME:
                                {
                                    string cmdstr = "设置无人机时间";
                                    success = pack.exData[0] == 0x01 ? true : false;
                                    if (success)
                                    {
                                        resp_msg = cmdstr + "成功";
                                    }
                                    else
                                    {
                                        resp_msg = cmdstr + "失败@错误码: " + pack.exData[1];
                                    }
                                }
                                break;
                            case CMD_TYPE_E.CMD_MOD_WIFI_SSID_PASSWD:
                                {
                                    string cmdstr = "修改wifi的ssid和密码";
                                    success = pack.exData[0] == 0x01 ? true : false;
                                    if (success)
                                    {
                                        resp_msg = cmdstr + "成功";
                                    }
                                    else
                                    {
                                        resp_msg = cmdstr + "失败@错误码: " + pack.exData[1];
                                    }
                                }
                                break;
                               
                            case CMD_TYPE_E.CMD_GET_UAV_PARAM:
                                {
                                    string cmdstr = "无人机基本信息获取";
                                    success = pack.exData[0] == 0x01 ? true : false;
                                    if (success)
                                    {
                                        resp_msg = cmdstr + "成功";
                                    }
                                    else
                                    {
                                        resp_msg = cmdstr + "失败@错误码: " + pack.exData[1];
                                    }
                                }
                                break;

                            case CMD_TYPE_E.CMD_PERMISSION_STATUS:
                                {                                    
                                    success = resp[4] == 0x01 ? true : false;
                                    if (success)
                                    {                                        
                                        MessageBox.Show("已有设备获得飞机操控权限");
                                    }                                    
                                }
                                break;

                            case CMD_TYPE_E.CMD_ROM_STATUS:
                                {                                    
                                    
                                    if (resp[4] == 1)
                                    {
                                        MessageBox.Show("emmc满");
                                    }
                                    else if (resp[4] == 2)
                                    {
                                        MessageBox.Show("sd卡满");
                                    }
                                    else if (resp[4] == 3)
                                    {
                                        MessageBox.Show("sd卡异常");
                                    }                                    
                                }
                                break;
                            default:
                                resp_msg = "未识别的返回信息！";
                                return;
                                break;
                        }
                    }
                } while (false);

                if (resp_msg != "")
                {
                    //MessageBox.Show(resp_msg);
                    this.Invoke((EventHandler)delegate
                    {
                        textBox1.Text = resp_msg;
                    });   
                }
                

                //打印16进制
                string resp_s = "";
                for (int i = 0; i < length; i++)
                {
                    resp_s += "0x" + resp_buff[i].ToString("x2") + " ";
                }

                this.Invoke((EventHandler)delegate
                {
                    rtxb_resp.Text = resp_s;
                });  
            }

        }   

        private void button1_Click(object sender, EventArgs e)
        {       
         
            if (is_playinig_ == false)
            {
                vlc_player_.Play(vlc_url, vlc_buff_time);
                is_playinig_ = true;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (is_playinig_)
            {
                vlc_player_.Stop();
                is_playinig_ = false;
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void 飞机设置ToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void 退出程序ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
        }

        private void 重置返航点ToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }


        private void planetSettingFormshow()
        {
            PlanetSetting myabout = new PlanetSetting();
            myabout.StartPosition = FormStartPosition.CenterScreen;

            if (myabout.ShowDialog() == DialogResult.OK)
            {
                udpServer = myabout.getIp();
                updPort = myabout.getPort();
                vlc_url = myabout.getUrl();
                vlc_buff_time = myabout.getBuffTime();
                //MessageBox.Show("飞机参数设置成功");
                init_udpClient();
                
            }
            myabout.Close();
        }

        private bool wifiSettingFormshow()
        {
            bool doSet = false;
            WifiSetting myabout = new WifiSetting();
            myabout.StartPosition = FormStartPosition.CenterScreen;

            if (myabout.ShowDialog() == DialogResult.OK)
            {
                wifiSsid = myabout.getSsid();
                wifiPasswd = myabout.getPasswd();
                doSet = true;             
            }
            myabout.Close();
            return doSet;
        }

        private void 参数设置ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            planetSettingFormshow();
        }

        private void txt_tcpIp_TextChanged(object sender, EventArgs e)
        {

        }

        private void txt_cmdDataMore_TextChanged(object sender, EventArgs e)
        {

        }

        private void groupBox2_Enter(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void button4_Click(object sender, EventArgs e)
        {

        }

        private void startrtpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txt_cmdDataMore.Enabled = true;
            txt_cmdDataMore.Text = "起流rtp";
            g_CmdType = CMD_TYPE_E.CMD_START_RTP;
            sendCmdToPlanet();
        }

        private void stoprtpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txt_cmdDataMore.Enabled = true;
            txt_cmdDataMore.Text = "停流rtp";
            g_CmdType = CMD_TYPE_E.CMD_STOP_RTP;
            sendCmdToPlanet();
        }

        private void takephotoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txt_cmdDataMore.Enabled = true;
            txt_cmdDataMore.Text = "拍照";

            g_CmdType = CMD_TYPE_E.CMD_TAKE_PHOTO;
            sendCmdToPlanet();
        }

        private void startrecordToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txt_cmdDataMore.Enabled = true;
            txt_cmdDataMore.Text = "开始录相";
            g_CmdType = CMD_TYPE_E.CMD_START_RECORD;
            sendCmdToPlanet();
        }

        private void stoprecordToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txt_cmdDataMore.Enabled = true;
            txt_cmdDataMore.Text = "停止录相";
            g_CmdType = CMD_TYPE_E.CMD_STOP_RECORD;
            sendCmdToPlanet();
        }
        private void unlockToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txt_cmdDataMore.Enabled = true;
            txt_cmdDataMore.Text = "一键解锁 unlock";
            g_CmdType = CMD_TYPE_E.CMD_CONTROL_UNLOCK;
            sendCmdToPlanet();
        }

        private void lockToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txt_cmdDataMore.Enabled = true;
            txt_cmdDataMore.Text = "一键加锁 lock";
            g_CmdType = CMD_TYPE_E.CMD_CONTROL_LOCK;
            sendCmdToPlanet();
        }


        private void takingoff一键起飞ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txt_cmdDataMore.Enabled = true;
            txt_cmdDataMore.Text = "一键起飞 taking_off ";
            g_CmdType = CMD_TYPE_E.CMD_CONTROL_TAKEING_OFF;
            sendCmdToPlanet();
        }

        private void landing一键降落ToolStripMenuItem_Click(object sender, EventArgs e)
        {
             
            txt_cmdDataMore.Enabled = true;
            txt_cmdDataMore.Text = "一键降落 landing ";
            g_CmdType = CMD_TYPE_E.CMD_CONTROL_TAKEING_LOADING;
            sendCmdToPlanet();
        }
        private void hover一键悬停ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txt_cmdDataMore.Enabled = true;
            txt_cmdDataMore.Text = "一键悬停 hover ";
            g_CmdType = CMD_TYPE_E.CMD_CONTROL_TAKEING_HOVER;
            sendCmdToPlanet();
        }

        private void returnback一键返航ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txt_cmdDataMore.Enabled = true;
            txt_cmdDataMore.Text = "一键返航 return_back ";
            g_CmdType = CMD_TYPE_E.CMD_CONTROL_TAKEING_RETURN_BACK;
            sendCmdToPlanet();
        }

        private void getspsppsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txt_cmdDataMore.Enabled = true;
            txt_cmdDataMore.Text = "获取sps pps数据 get_sps_pps";
            g_CmdType = CMD_TYPE_E.CMD_GET_SPS_PPS;
            sendCmdToPlanet();
        }

        private void 获取录像时间GetrecordvideotimeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txt_cmdDataMore.Enabled = true;
            txt_cmdDataMore.Text = "获取录像时间 get_record_video_time";
            g_CmdType = CMD_TYPE_E.CMD_GET_RECORD_VIDEO_TIME;
            sendCmdToPlanet();
        }

        private void 获取存储空间剩余录像时间GetrecordvideofreetimeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txt_cmdDataMore.Enabled = true;
            txt_cmdDataMore.Text = "获取存储空间剩余录像时间 get_record_video_free_time";
            g_CmdType = CMD_TYPE_E.CMD_GET_RECORD_VIDEO_FREE_TIME;
            sendCmdToPlanet();
        }

        private void 获取剩余容量GetfreespaceToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            txt_cmdDataMore.Enabled = true;
            txt_cmdDataMore.Text = "获取剩余容量 get_free_space";
            g_CmdType = CMD_TYPE_E.CMD_GET_FREE_SPACE;
            sendCmdToPlanet();
        }

        private void picpathToolStripMenuItem_Click(object sender, EventArgs e)
        {
            clear_save_path_var = 0;
            txt_cmdDataMore.Enabled = true;
            txt_cmdDataMore.Text = "清空存储目录 clear_save_path";
            g_CmdType = CMD_TYPE_E.CMD_CLEAR_SAVE_PATH;
            sendCmdToPlanet();
        }

        private void videopathToolStripMenuItem_Click(object sender, EventArgs e)
        {
            clear_save_path_var = 1;
            txt_cmdDataMore.Enabled = true;
            txt_cmdDataMore.Text = "清空存储目录 clear_save_path";
            g_CmdType = CMD_TYPE_E.CMD_CLEAR_SAVE_PATH;
            sendCmdToPlanet();
        }

        private void 查询无人机时间GetUAVtimeToolStripMenuItem_Click(object sender, EventArgs e)
        {                       
            txt_cmdDataMore.Enabled = true;
            txt_cmdDataMore.Text = "查询无人机时间 get_UAV_time";
            g_CmdType = CMD_TYPE_E.CMD_GET_UAV_TIME;
            sendCmdToPlanet();
        }

        private void 设置无人机时间SetUAVtimeToolStripMenuItem_Click(object sender, EventArgs e)
        {                                    
            
            txt_cmdDataMore.Enabled = true;
            txt_cmdDataMore.Text = "设置无人机时间 set_UAV_time";
            g_CmdType = CMD_TYPE_E.CMD_SET_UAV_TIME;
            sendCmdToPlanet();
        }

        private void 无人机基本信息获取GetUAVparamToolStripMenuItem_Click(object sender, EventArgs e)
        {                        
            txt_cmdDataMore.Enabled = true;
            txt_cmdDataMore.Text = "无人机基本信息获取 get_UAV_param";
            g_CmdType = CMD_TYPE_E.CMD_GET_UAV_PARAM;
            sendCmdToPlanet();
        }

        private void 修改密码及WIFIToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txt_cmdDataMore.Enabled = true;
            txt_cmdDataMore.Text = "修改WIFI及密码 modify_wifi_ssid_and_password";
            g_CmdType = CMD_TYPE_E.CMD_MOD_WIFI_SSID_PASSWD;
            bool ret = wifiSettingFormshow();
      
            if(ret)
            {
                sendCmdToPlanet();
            }
        }

        private void pToolStripMenuItem_Click(object sender, EventArgs e)
        {
            preview_resolution = 0;
            resolution_type = 0;
            this.pToolStripMenuItem.Checked = true;
            this.pToolStripMenuItem1.Checked = false;
            this.pToolStripMenuItem2.Checked = false;
            txt_cmdDataMore.Text = "设置FPV预览分辨率 1280*720p";
            g_CmdType = CMD_TYPE_E.CMD_SET_PREVIEW_RESOLUTION;
            sendCmdToPlanet();
        }

        private void pToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            preview_resolution = 1;
            resolution_type = 0;
            this.pToolStripMenuItem.Checked = false;
            this.pToolStripMenuItem1.Checked = true;
            this.pToolStripMenuItem2.Checked = false;
            txt_cmdDataMore.Text = "设置FPV预览分辨率  640*480p";
            g_CmdType = CMD_TYPE_E.CMD_SET_PREVIEW_RESOLUTION;
            sendCmdToPlanet();
        }
        private void pToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            preview_resolution = 2;
            resolution_type = 0;
            this.pToolStripMenuItem.Checked = false;
            this.pToolStripMenuItem1.Checked = false;
            this.pToolStripMenuItem2.Checked = true;
            txt_cmdDataMore.Enabled = true;

            txt_cmdDataMore.Text = "设置FPV预览分辨率  320*240p";
            g_CmdType = CMD_TYPE_E.CMD_SET_PREVIEW_RESOLUTION;
            sendCmdToPlanet();
        }

        private void fpsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            video_resolution = 2;
            resolution_type = 1;
            this.fpsToolStripMenuItem.Checked = true;
            this.fpsToolStripMenuItem1.Checked = false;

            txt_cmdDataMore.Text = "设置录像分辨率  1920*1080 30fps";
            g_CmdType = CMD_TYPE_E.CMD_SET_PREVIEW_RESOLUTION;
            sendCmdToPlanet();
        }

        private void fpsToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            video_resolution = 1;
            resolution_type = 1;
            this.fpsToolStripMenuItem.Checked = false;
            this.fpsToolStripMenuItem1.Checked = true;

            txt_cmdDataMore.Text = "设置录像分辨率  1280*720 30fps";
            g_CmdType = CMD_TYPE_E.CMD_SET_PREVIEW_RESOLUTION;
            sendCmdToPlanet();
        }

        private void 飞控指令ToolStripMenuItem_Click(object sender, EventArgs e)
        {
             
        }

        private void 获取Camera基础参数GetcamerabasicparametersToolStripMenuItem_Click(object sender, EventArgs e)
        {                                     
            txt_cmdDataMore.Enabled = true;
            txt_cmdDataMore.Text = "获取Camera基础参数 get_camera_basic_parameters";
            g_CmdType = CMD_TYPE_E.CMD_GET_CAMERA_BASIC_PARAM;
            sendCmdToPlanet();
        }

        private void 自动ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            camera_param_detail_type = 0;
            camera_set_awb_type = 0;
            自动ToolStripMenuItem.Checked = true;
            晴天ToolStripMenuItem.Checked = false;
            阴天ToolStripMenuItem.Checked = false;
            白炽灯ToolStripMenuItem.Checked = false;
            荧光灯ToolStripMenuItem.Checked = false;
            txt_cmdDataMore.Text = "设置AWB  自动";
            g_CmdType = CMD_TYPE_E.CMD_CAMERA_PARAM_DETAIL;
            sendCmdToPlanet();
           
        }

        private void 晴天ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            camera_param_detail_type = 0;
            camera_set_awb_type = 4;
            自动ToolStripMenuItem.Checked = false;
            晴天ToolStripMenuItem.Checked = true;
            阴天ToolStripMenuItem.Checked = false;
            白炽灯ToolStripMenuItem.Checked = false;
            荧光灯ToolStripMenuItem.Checked = false;
            txt_cmdDataMore.Text = "设置AWB   晴天";
            g_CmdType = CMD_TYPE_E.CMD_CAMERA_PARAM_DETAIL;
            sendCmdToPlanet();
        }

        private void 阴天ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            camera_param_detail_type = 0;
            camera_set_awb_type = 5;
            自动ToolStripMenuItem.Checked = false;
            晴天ToolStripMenuItem.Checked = false;
            阴天ToolStripMenuItem.Checked = true;
            白炽灯ToolStripMenuItem.Checked = false;
            荧光灯ToolStripMenuItem.Checked = false;
            txt_cmdDataMore.Text = "设置AWB   阴天";
            g_CmdType = CMD_TYPE_E.CMD_CAMERA_PARAM_DETAIL;
            sendCmdToPlanet();
        }

        private void 白炽灯ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            camera_param_detail_type = 0;
            camera_set_awb_type = 1;
            自动ToolStripMenuItem.Checked = false;
            晴天ToolStripMenuItem.Checked = false;
            阴天ToolStripMenuItem.Checked = false;
            白炽灯ToolStripMenuItem.Checked = true;
            荧光灯ToolStripMenuItem.Checked = false;
            txt_cmdDataMore.Text = "设置AWB   白炽灯";
            g_CmdType = CMD_TYPE_E.CMD_CAMERA_PARAM_DETAIL;
            sendCmdToPlanet();
        }

        private void 荧光灯ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            camera_param_detail_type = 0;
            camera_set_awb_type = 3;
            自动ToolStripMenuItem.Checked = false;
            晴天ToolStripMenuItem.Checked = false;
            阴天ToolStripMenuItem.Checked = false;
            白炽灯ToolStripMenuItem.Checked = false;
            荧光灯ToolStripMenuItem.Checked = true;
            txt_cmdDataMore.Text = "设置AWB   荧光灯";
            g_CmdType = CMD_TYPE_E.CMD_CAMERA_PARAM_DETAIL;
            sendCmdToPlanet();
        }

        private void autoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            camera_param_detail_type = 1;
            camera_set_iso_type = 0;
            autoToolStripMenuItem.Checked = true;
            toolStripMenuItem5.Checked = false;
            toolStripMenuItem6.Checked = false;
            toolStripMenuItem7.Checked = false;
            toolStripMenuItem8.Checked = false;
            toolStripMenuItem9.Checked = false;
            toolStripMenuItem10.Checked = false;
            txt_cmdDataMore.Text = "设置ISO   auto";
            g_CmdType = CMD_TYPE_E.CMD_CAMERA_PARAM_DETAIL;
            sendCmdToPlanet();
        }

        private void toolStripMenuItem5_Click(object sender, EventArgs e)
        {
            camera_param_detail_type = 1;
            camera_set_iso_type = 1;
            autoToolStripMenuItem.Checked = false;
            toolStripMenuItem5.Checked = true;
            toolStripMenuItem6.Checked = false;
            toolStripMenuItem7.Checked = false;
            toolStripMenuItem8.Checked = false;
            toolStripMenuItem9.Checked = false;
            toolStripMenuItem10.Checked = false;
            txt_cmdDataMore.Text = "设置ISO   100";
            g_CmdType = CMD_TYPE_E.CMD_CAMERA_PARAM_DETAIL;
            sendCmdToPlanet();
        }

        private void toolStripMenuItem6_Click(object sender, EventArgs e)
        {
            camera_param_detail_type = 1;
            camera_set_iso_type = 2;
            autoToolStripMenuItem.Checked = false;
            toolStripMenuItem5.Checked = false;
            toolStripMenuItem6.Checked = true;
            toolStripMenuItem7.Checked = false;
            toolStripMenuItem8.Checked = false;
            toolStripMenuItem9.Checked = false;
            toolStripMenuItem10.Checked = false;
            txt_cmdDataMore.Text = "设置ISO   200";
            g_CmdType = CMD_TYPE_E.CMD_CAMERA_PARAM_DETAIL;
            sendCmdToPlanet();
        }

        private void toolStripMenuItem7_Click(object sender, EventArgs e)
        {
            camera_param_detail_type = 1;
            camera_set_iso_type = 3;
            autoToolStripMenuItem.Checked = false;
            toolStripMenuItem5.Checked = false;
            toolStripMenuItem6.Checked = false;
            toolStripMenuItem7.Checked = true;
            toolStripMenuItem8.Checked = false;
            toolStripMenuItem9.Checked = false;
            toolStripMenuItem10.Checked = false;
            txt_cmdDataMore.Text = "设置ISO   400";
            g_CmdType = CMD_TYPE_E.CMD_CAMERA_PARAM_DETAIL;
            sendCmdToPlanet();
        }

        private void toolStripMenuItem8_Click(object sender, EventArgs e)
        {
            camera_param_detail_type = 1;
            camera_set_iso_type = 4;
            autoToolStripMenuItem.Checked = false;
            toolStripMenuItem5.Checked = false;
            toolStripMenuItem6.Checked = false;
            toolStripMenuItem7.Checked = false;
            toolStripMenuItem8.Checked = true;
            toolStripMenuItem9.Checked = false;
            toolStripMenuItem10.Checked = false;
            txt_cmdDataMore.Text = "设置ISO   800";
            g_CmdType = CMD_TYPE_E.CMD_CAMERA_PARAM_DETAIL;
            sendCmdToPlanet();
        }

        private void toolStripMenuItem9_Click(object sender, EventArgs e)
        {
            camera_param_detail_type = 1;
            camera_set_iso_type = 5;
            autoToolStripMenuItem.Checked = false;
            toolStripMenuItem5.Checked = false;
            toolStripMenuItem6.Checked = false;
            toolStripMenuItem7.Checked = false;
            toolStripMenuItem8.Checked = false;
            toolStripMenuItem9.Checked = true;
            toolStripMenuItem10.Checked = false;
            txt_cmdDataMore.Text = "设置ISO   1600";
            g_CmdType = CMD_TYPE_E.CMD_CAMERA_PARAM_DETAIL;
            sendCmdToPlanet();
        }

        private void toolStripMenuItem10_Click(object sender, EventArgs e)
        {
            camera_param_detail_type = 1;
            camera_set_iso_type = 6;
            autoToolStripMenuItem.Checked = false;
            toolStripMenuItem5.Checked = false;
            toolStripMenuItem6.Checked = false;
            toolStripMenuItem7.Checked = false;
            toolStripMenuItem8.Checked = false;
            toolStripMenuItem9.Checked = false;
            toolStripMenuItem10.Checked = true;
            txt_cmdDataMore.Text = "设置ISO   3200";
            g_CmdType = CMD_TYPE_E.CMD_CAMERA_PARAM_DETAIL;
            sendCmdToPlanet();
        }

        private void toolStripMenuItem11_Click(object sender, EventArgs e)
        {
            camera_param_detail_type = 2;
            camera_set_ev_type = 0;
            toolStripMenuItem11.Checked = true;
            toolStripMenuItem12.Checked = false;
            toolStripMenuItem13.Checked = false;
            toolStripMenuItem14.Checked = false;
            toolStripMenuItem15.Checked = false;
            toolStripMenuItem16.Checked = false;
            toolStripMenuItem17.Checked = false;
            txt_cmdDataMore.Text = "设置EV   0";
            g_CmdType = CMD_TYPE_E.CMD_CAMERA_PARAM_DETAIL;
            sendCmdToPlanet();
        }

        private void toolStripMenuItem12_Click(object sender, EventArgs e)
        {
            camera_param_detail_type = 2;
            camera_set_ev_type = 1;
            toolStripMenuItem11.Checked = false;
            toolStripMenuItem12.Checked = true;
            toolStripMenuItem13.Checked = false;
            toolStripMenuItem14.Checked = false;
            toolStripMenuItem15.Checked = false;
            toolStripMenuItem16.Checked = false;
            toolStripMenuItem17.Checked = false;
            txt_cmdDataMore.Text = "设置EV   -6";
            g_CmdType = CMD_TYPE_E.CMD_CAMERA_PARAM_DETAIL;
            sendCmdToPlanet();
        }

        private void toolStripMenuItem13_Click(object sender, EventArgs e)
        {
            camera_param_detail_type = 2;
            camera_set_ev_type = 2;
            toolStripMenuItem11.Checked = false;
            toolStripMenuItem12.Checked = false;
            toolStripMenuItem13.Checked = true;
            toolStripMenuItem14.Checked = false;
            toolStripMenuItem15.Checked = false;
            toolStripMenuItem16.Checked = false;
            toolStripMenuItem17.Checked = false;
            txt_cmdDataMore.Text = "设置EV   -4";
            g_CmdType = CMD_TYPE_E.CMD_CAMERA_PARAM_DETAIL;
            sendCmdToPlanet();
        }

        private void toolStripMenuItem14_Click(object sender, EventArgs e)
        {
            camera_param_detail_type = 2;
            camera_set_ev_type = 3;
            toolStripMenuItem11.Checked = false;
            toolStripMenuItem12.Checked = false;
            toolStripMenuItem13.Checked = false;
            toolStripMenuItem14.Checked = true;
            toolStripMenuItem15.Checked = false;
            toolStripMenuItem16.Checked = false;
            toolStripMenuItem17.Checked = false;
            txt_cmdDataMore.Text = "设置EV   -2";
            g_CmdType = CMD_TYPE_E.CMD_CAMERA_PARAM_DETAIL;
            sendCmdToPlanet();
        }

        private void toolStripMenuItem15_Click(object sender, EventArgs e)
        {
            camera_param_detail_type = 2;
            camera_set_ev_type = 4;
            toolStripMenuItem11.Checked = false;
            toolStripMenuItem12.Checked = false;
            toolStripMenuItem13.Checked = false;
            toolStripMenuItem14.Checked = false;
            toolStripMenuItem15.Checked = true;
            toolStripMenuItem16.Checked = false;
            toolStripMenuItem17.Checked = false;
            txt_cmdDataMore.Text = "设置EV   2";
            g_CmdType = CMD_TYPE_E.CMD_CAMERA_PARAM_DETAIL;
            sendCmdToPlanet();
        }

        private void toolStripMenuItem16_Click(object sender, EventArgs e)
        {
            camera_param_detail_type = 2;
            camera_set_ev_type = 5;
            toolStripMenuItem11.Checked = false;
            toolStripMenuItem12.Checked = false;
            toolStripMenuItem13.Checked = false;
            toolStripMenuItem14.Checked = false;
            toolStripMenuItem15.Checked = false;
            toolStripMenuItem16.Checked = true;
            toolStripMenuItem17.Checked = false;
            txt_cmdDataMore.Text = "设置EV   4";
            g_CmdType = CMD_TYPE_E.CMD_CAMERA_PARAM_DETAIL;
            sendCmdToPlanet();
        }

        private void toolStripMenuItem17_Click(object sender, EventArgs e)
        {
            camera_param_detail_type = 2;
            camera_set_ev_type = 6;
            toolStripMenuItem11.Checked = false;
            toolStripMenuItem12.Checked = false;
            toolStripMenuItem13.Checked = false;
            toolStripMenuItem14.Checked = false;
            toolStripMenuItem15.Checked = false;
            toolStripMenuItem16.Checked = false;
            toolStripMenuItem17.Checked = true;
            txt_cmdDataMore.Text = "设置EV   6";
            g_CmdType = CMD_TYPE_E.CMD_CAMERA_PARAM_DETAIL;
            sendCmdToPlanet();

        }

        private void toolStripMenuItem18_Click(object sender, EventArgs e)
        {                            
            camera_param_detail_type = 3;
            camera_set_take_photo_count = 3;
            toolStripMenuItem18.Checked = true;
            toolStripMenuItem19.Checked = false;
            toolStripMenuItem20.Checked = false;
            toolStripMenuItem21.Checked = false;
            toolStripMenuItem22.Checked = false;
            toolStripMenuItem23.Checked = false;            
            txt_cmdDataMore.Text = "设置连拍张数   3";
            g_CmdType = CMD_TYPE_E.CMD_CAMERA_PARAM_DETAIL;
            sendCmdToPlanet();
        }

        private void toolStripMenuItem19_Click(object sender, EventArgs e)
        {
            camera_param_detail_type = 3;
            camera_set_take_photo_count = 5;
            toolStripMenuItem18.Checked = false;
            toolStripMenuItem19.Checked = true;
            toolStripMenuItem20.Checked = false;
            toolStripMenuItem21.Checked = false;
            toolStripMenuItem22.Checked = false;
            toolStripMenuItem23.Checked = false;
            txt_cmdDataMore.Text = "设置连拍张数  5";
            g_CmdType = CMD_TYPE_E.CMD_CAMERA_PARAM_DETAIL;
            sendCmdToPlanet();
        }

        private void toolStripMenuItem20_Click(object sender, EventArgs e)
        {
            camera_param_detail_type = 3;
            camera_set_take_photo_count = 7;
            toolStripMenuItem18.Checked = false;
            toolStripMenuItem19.Checked = false;
            toolStripMenuItem20.Checked = true;
            toolStripMenuItem21.Checked = false;
            toolStripMenuItem22.Checked = false;
            toolStripMenuItem23.Checked = false;
            txt_cmdDataMore.Text = "设置连拍张数   7";
            g_CmdType = CMD_TYPE_E.CMD_CAMERA_PARAM_DETAIL;
            sendCmdToPlanet();
        }

        private void toolStripMenuItem21_Click(object sender, EventArgs e)
        {
            camera_param_detail_type = 3;
            camera_set_take_photo_count = 9;
            toolStripMenuItem18.Checked = false;
            toolStripMenuItem19.Checked = false;
            toolStripMenuItem20.Checked = false;
            toolStripMenuItem21.Checked = true;
            toolStripMenuItem22.Checked = false;
            toolStripMenuItem23.Checked = false;
            txt_cmdDataMore.Text = "设置连拍张数   9";
            g_CmdType = CMD_TYPE_E.CMD_CAMERA_PARAM_DETAIL;
            sendCmdToPlanet();
        }

        private void toolStripMenuItem22_Click(object sender, EventArgs e)
        {
            camera_param_detail_type = 3;
            camera_set_take_photo_count = 13;
            toolStripMenuItem18.Checked = false;
            toolStripMenuItem19.Checked = false;
            toolStripMenuItem20.Checked = false;
            toolStripMenuItem21.Checked = false;
            toolStripMenuItem22.Checked = true;
            toolStripMenuItem23.Checked = false;
            txt_cmdDataMore.Text = "设置连拍张数   13";
            g_CmdType = CMD_TYPE_E.CMD_CAMERA_PARAM_DETAIL;
            sendCmdToPlanet();
        }

        private void toolStripMenuItem23_Click(object sender, EventArgs e)
        {
            camera_param_detail_type = 3;
            camera_set_take_photo_count = 15;
            toolStripMenuItem18.Checked = false;
            toolStripMenuItem19.Checked = false;
            toolStripMenuItem20.Checked = false;
            toolStripMenuItem21.Checked = false;
            toolStripMenuItem22.Checked = false;
            toolStripMenuItem23.Checked = true;
            txt_cmdDataMore.Text = "设置连拍张数   15";
            g_CmdType = CMD_TYPE_E.CMD_CAMERA_PARAM_DETAIL;
            sendCmdToPlanet();
        }

        private void sToolStripMenuItem_Click(object sender, EventArgs e)
        {
            camera_param_detail_type = 4;
            camera_set_delay_time = 3;
            sToolStripMenuItem.Checked = true;
            sToolStripMenuItem1.Checked = false;
            sToolStripMenuItem2.Checked = false;
            sToolStripMenuItem3.Checked = false;
            sToolStripMenuItem4.Checked = false;            
            txt_cmdDataMore.Text = "设置延时拍照时间   3s";
            g_CmdType = CMD_TYPE_E.CMD_CAMERA_PARAM_DETAIL;
            sendCmdToPlanet();
        }

        private void sToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            camera_param_detail_type = 4;
            camera_set_delay_time = 5;
            sToolStripMenuItem.Checked = false;
            sToolStripMenuItem1.Checked = true;
            sToolStripMenuItem2.Checked = false;
            sToolStripMenuItem3.Checked = false;
            sToolStripMenuItem4.Checked = false;  
            txt_cmdDataMore.Text = "设置延时拍照时间   5s";
            g_CmdType = CMD_TYPE_E.CMD_CAMERA_PARAM_DETAIL;
            sendCmdToPlanet();
        }

        private void sToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            camera_param_detail_type = 4;
            camera_set_delay_time = 7;
            sToolStripMenuItem.Checked = false;
            sToolStripMenuItem1.Checked = false;
            sToolStripMenuItem2.Checked = true;
            sToolStripMenuItem3.Checked = false;
            sToolStripMenuItem4.Checked = false;  
            txt_cmdDataMore.Text = "设置延时拍照时间   7s";
            g_CmdType = CMD_TYPE_E.CMD_CAMERA_PARAM_DETAIL;
            sendCmdToPlanet();
        }

        private void sToolStripMenuItem3_Click(object sender, EventArgs e)
        {
            camera_param_detail_type = 4;
            camera_set_delay_time = 10;
            sToolStripMenuItem.Checked = false;
            sToolStripMenuItem1.Checked = false;
            sToolStripMenuItem2.Checked = false;
            sToolStripMenuItem3.Checked = true;
            sToolStripMenuItem4.Checked = false;  
            txt_cmdDataMore.Text = "设置延时拍照时间   10s";
            g_CmdType = CMD_TYPE_E.CMD_CAMERA_PARAM_DETAIL;
            sendCmdToPlanet();
        }

        private void sToolStripMenuItem4_Click(object sender, EventArgs e)
        {
            camera_param_detail_type = 4;
            camera_set_delay_time = 20;
            sToolStripMenuItem.Checked = false;
            sToolStripMenuItem1.Checked = false;
            sToolStripMenuItem2.Checked = false;
            sToolStripMenuItem3.Checked = false;
            sToolStripMenuItem4.Checked = true;  
            txt_cmdDataMore.Text = "设置延时拍照时间   20s";
            g_CmdType = CMD_TYPE_E.CMD_CAMERA_PARAM_DETAIL;
            sendCmdToPlanet();
        }

        private void 获取Camera详细参数GetcameradescriptionparamToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
            txt_cmdDataMore.Enabled = true;
            txt_cmdDataMore.Text = "获取Camera详细参数 get_camera_description_param";
            g_CmdType = CMD_TYPE_E.CMD_CAMERA_DESC_PARAM_DETAIL;
            sendCmdToPlanet();
        }

        private void 查询存储路径GetsavepathToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txt_cmdDataMore.Enabled = true;
            txt_cmdDataMore.Text = "查询存储路径 get_save_path";
            g_CmdType = CMD_TYPE_E.CMD_GET_SAVE_PATH;
            sendCmdToPlanet();
        }

        private void emmcToolStripMenuItem_Click(object sender, EventArgs e)
        {
            camera_set_save_path_type = 0;
            emmcToolStripMenuItem.Checked = true;
            sdcardToolStripMenuItem.Checked = false;
            txt_cmdDataMore.Enabled = true;
            txt_cmdDataMore.Text = "设置存储路径 set_save_path emmc";
            g_CmdType = CMD_TYPE_E.CMD_SET_SAVE_PATH;
            sendCmdToPlanet();
        }

        private void sdcardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            camera_set_save_path_type = 1;
            emmcToolStripMenuItem.Checked = false;
            sdcardToolStripMenuItem.Checked = true;
            txt_cmdDataMore.Enabled = true;
            txt_cmdDataMore.Text = "设置存储路径 set_save_path sd_card ";
            g_CmdType = CMD_TYPE_E.CMD_SET_SAVE_PATH;
            sendCmdToPlanet();
        }

        private void 开始校准ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txt_cmdDataMore.Enabled = true;
            txt_cmdDataMore.Text = "开始指南针效验  start_check_compass ";
            g_CmdType = CMD_TYPE_E.CMD_START_CHECK_COMPASS;
            compass_status = -1;
            sendCmdToPlanet();
        }

        private void button4_Click_1(object sender, EventArgs e)
        {
            txt_cmdDataMore.Enabled = true;
            txt_cmdDataMore.Text = "拍照";
            g_CmdType = CMD_TYPE_E.CMD_TAKE_PHOTO;
            sendCmdToPlanet();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            txt_cmdDataMore.Enabled = true;
            txt_cmdDataMore.Text = "开始录相";
            g_CmdType = CMD_TYPE_E.CMD_START_RECORD;
            sendCmdToPlanet();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            txt_cmdDataMore.Enabled = true;
            txt_cmdDataMore.Text = "开始停止";
            g_CmdType = CMD_TYPE_E.CMD_STOP_RECORD;
            sendCmdToPlanet();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            txt_cmdDataMore.Enabled = true;
            txt_cmdDataMore.Text = "开始视频跟随 start_image_follow";
            g_CmdType = CMD_TYPE_E.CMD_START_IMAGE_FOLLOW;
            sendCmdToPlanet();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            txt_cmdDataMore.Enabled = true;
            txt_cmdDataMore.Text = "停止视频跟随 stop_image_follow";
            g_CmdType = CMD_TYPE_E.CMD_START_IMAGE_FOLLOW ;
            sendCmdToPlanet();
        }

        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void 连接ToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            picture_size = 0;
            resolution_type = 2;
            this.toolStripMenuItem2.Checked = true;
            this.toolStripMenuItem3.Checked = false;
            this.toolStripMenuItem4.Checked = false;
            txt_cmdDataMore.Text = "设置拍照分辨率  16:9(3840*2160)";
            g_CmdType = CMD_TYPE_E.CMD_SET_PREVIEW_RESOLUTION;
            sendCmdToPlanet();
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            picture_size = 1;
            resolution_type = 2;
            this.toolStripMenuItem2.Checked = false;
            this.toolStripMenuItem3.Checked = true;
            this.toolStripMenuItem4.Checked = false;
            txt_cmdDataMore.Text = "设置拍照分辨率  4:3(4208*3120)";
            g_CmdType = CMD_TYPE_E.CMD_SET_PREVIEW_RESOLUTION;
            sendCmdToPlanet();
        }

        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            picture_size = 0;
            resolution_type = 2;
            this.toolStripMenuItem2.Checked = false;
            this.toolStripMenuItem3.Checked = false;
            this.toolStripMenuItem4.Checked = true;
            txt_cmdDataMore.Text = "设置拍照分辨率 1:1(3120*3120)";
            g_CmdType = CMD_TYPE_E.CMD_SET_PREVIEW_RESOLUTION;
            sendCmdToPlanet();
        }
        private void 单拍ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            take_mode = 0;
            resolution_type = 3;
            this.单拍ToolStripMenuItem.Checked = true;
            this.连拍ToolStripMenuItem.Checked = false;
            this.延时拍ToolStripMenuItem.Checked = false;
            txt_cmdDataMore.Text = "设置拍照模式 单拍";
            g_CmdType = CMD_TYPE_E.CMD_SET_PREVIEW_RESOLUTION;
            sendCmdToPlanet();
        }
        private void 连拍ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            take_mode = 1;
            resolution_type = 3;
            this.单拍ToolStripMenuItem.Checked = false;
            this.连拍ToolStripMenuItem.Checked = true;
            this.延时拍ToolStripMenuItem.Checked = false;
            txt_cmdDataMore.Text = "设置拍照模式 连拍 ";
            g_CmdType = CMD_TYPE_E.CMD_SET_PREVIEW_RESOLUTION;
            sendCmdToPlanet();   
            
        }

        private void 延时拍ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            take_mode = 2;
            resolution_type = 3;
            this.单拍ToolStripMenuItem.Checked = false;
            this.连拍ToolStripMenuItem.Checked = false;
            this.延时拍ToolStripMenuItem.Checked = true;
            txt_cmdDataMore.Text = "设置拍照模式 延时拍照";
            g_CmdType = CMD_TYPE_E.CMD_SET_PREVIEW_RESOLUTION;
            sendCmdToPlanet();
            
        }

        private void ExporeMini_Test_FormClosed(object sender, FormClosedEventArgs e)
        {
            System.Environment.Exit(0);
        }
    }
}

