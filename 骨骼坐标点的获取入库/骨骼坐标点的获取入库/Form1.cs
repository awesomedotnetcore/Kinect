﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Windows;
using Microsoft.Kinect;
using System.Data.SqlClient;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;



namespace 骨骼坐标点的获取入库//不好意思命名我用了汉字。
{

    public partial class Form1 : Form
    {
        FileStream fs = new FileStream("f:\\2.txt", FileMode.Create);
        StreamWriter sw;
        private const string V = "F:\\kinect\\12.jpg";
        private String connsql = "server=.;database=bone_pos;integrated security=SSPI";
        private Image<Bgr, Byte> skeletonImage;
        int depthWidth, depthHeight;

        private Skeleton[] skeletonData;//按理说是识别六人，size = 6
        private MCvFont font = new MCvFont(Emgu.CV.CvEnum.FONT.CV_FONT_HERSHEY_COMPLEX, 0.3, 0.3);
        //数据缓冲存储空间
        ColorImageFormat colorImageFormat;
        DepthImageFormat depthImageFormat;

        private KinectSensor sensor;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            pictureBox1.Load(V);
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }//连接设备;
            if (null != this.sensor)
            {   //初始化Kinect设置     
                sw = new StreamWriter(this.fs);
                colorImageFormat = ColorImageFormat.RgbResolution640x480Fps30;
                depthImageFormat = DepthImageFormat.Resolution640x480Fps30;
                this.sensor.SkeletonStream.Enable();//设备骨骼可用;

                this.sensor.ColorStream.Enable(colorImageFormat);
                this.sensor.DepthStream.Enable(depthImageFormat);
                depthWidth = this.sensor.DepthStream.FrameWidth;
                depthHeight = this.sensor.DepthStream.FrameHeight;
                skeletonImage = new Image<Bgr, byte>(depthWidth, depthHeight);
                skeletonImage.Draw(new Rectangle(0, 0, depthWidth, depthHeight), new Bgr(0.0, 0.0, 0.0), -1);
                imageBox1.Image = skeletonImage;
                this.skeletonData = new Skeleton[this.sensor.SkeletonStream.FrameSkeletonArrayLength];

                this.sensor.SkeletonFrameReady += SensorSkeletonFrameReady;//事件处理;
                try
                {
                    this.sensor.Start();
                }
                catch
                {
                    ;
                }
            }
            else
            {
                MessageBox.Show("设备未就绪！", "未连接");
            }

        }

        private void SensorSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            //Skeleton[] skeletonData = new Skeleton[0];
            //using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            //{
            //    skeletonData = new Skeleton[skeletonFrame.SkeletonArrayLength];
            //    skeletonFrame.CopySkeletonDataTo(skeletonData);

            //}
            bool received = false;

            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletonFrame.CopySkeletonDataTo(this.skeletonData);
                    received = true;
                    ////=================================
                    
                    //    sw.Write(" 头部 x " + skeletonData[0].Joints[JointType.Head].Position.X.ToString());
                    //    sw.Write(" y " + skeletonData[0].Joints[JointType.Head].Position.Y.ToString());
                    //    sw.Write(" z " + skeletonData[0].Joints[JointType.Head].Position.Z.ToString());
                    //    sw.Write(" 左手  x " + skeletonData[0].Joints[JointType.HandLeft].Position.X.ToString());
                    //    sw.Write(" y " + skeletonData[0].Joints[JointType.HandLeft].Position.Y.ToString());
                    //    sw.WriteLine(" z " + skeletonData[0].Joints[JointType.HandLeft].Position.Z.ToString());
                    //    sw.Flush();
                    //    for(int i = 0; i < 6; i++)
                    //    {
                    //        if (skeletonData[i].Joints[JointType.HandLeft].Position.Z != 0)
                    //        {
                    //            Console.Write(i);
                    //            break;
                    //        }
                    //    }
                    
                    //=================================
                }
            }

            if (received)
            {
                //重绘整个画面，冲掉原有骨骼图像
                skeletonImage.Draw(new Rectangle(0, 0, skeletonImage.Width, skeletonImage.Height), new Bgr(0.0, 0.0, 0.0), -1);

                DrawSkeletons(skeletonImage, 0);
                imageBox1.Image = skeletonImage;
            }

        }
        private void DrawSkeletons(Image<Bgr, Byte> img, int depthOrColor)
        {
            //绘制所有正确Tracked的骨骼
            foreach (Skeleton skeleton in this.skeletonData)
            {
                if (skeleton == null) continue;
                if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
                {
                    DrawTrackedSkeletonJoints(img, skeleton.Joints, depthOrColor);
                }
            }
        }
        private void DrawTrackedSkeletonJoints(Image<Bgr, Byte> img, JointCollection jointCollection, int depthOrColor)
        {
            // Render Head and Shoulders
            DrawBone(img, jointCollection[JointType.Head], jointCollection[JointType.ShoulderCenter], depthOrColor);
            DrawBone(img, jointCollection[JointType.ShoulderCenter], jointCollection[JointType.ShoulderLeft], depthOrColor);
            DrawBone(img, jointCollection[JointType.ShoulderCenter], jointCollection[JointType.ShoulderRight], depthOrColor);

            // Render Left Arm
            DrawBone(img, jointCollection[JointType.ShoulderLeft], jointCollection[JointType.ElbowLeft], depthOrColor);
            DrawBone(img, jointCollection[JointType.ElbowLeft], jointCollection[JointType.WristLeft], depthOrColor);
            DrawBone(img, jointCollection[JointType.WristLeft], jointCollection[JointType.HandLeft], depthOrColor);

            // Render Right Arm
            DrawBone(img, jointCollection[JointType.ShoulderRight], jointCollection[JointType.ElbowRight], depthOrColor);
            DrawBone(img, jointCollection[JointType.ElbowRight], jointCollection[JointType.WristRight], depthOrColor);
            DrawBone(img, jointCollection[JointType.WristRight], jointCollection[JointType.HandRight], depthOrColor);

            // Render other bones...
            DrawBone(img, jointCollection[JointType.ShoulderCenter], jointCollection[JointType.Spine], depthOrColor);

            DrawBone(img, jointCollection[JointType.Spine], jointCollection[JointType.HipRight], depthOrColor);
            DrawBone(img, jointCollection[JointType.KneeRight], jointCollection[JointType.HipRight], depthOrColor);
            DrawBone(img, jointCollection[JointType.KneeRight], jointCollection[JointType.AnkleRight], depthOrColor);
            DrawBone(img, jointCollection[JointType.FootRight], jointCollection[JointType.AnkleRight], depthOrColor);

            DrawBone(img, jointCollection[JointType.Spine], jointCollection[JointType.HipLeft], depthOrColor);
            DrawBone(img, jointCollection[JointType.KneeLeft], jointCollection[JointType.HipLeft], depthOrColor);
            DrawBone(img, jointCollection[JointType.KneeLeft], jointCollection[JointType.AnkleLeft], depthOrColor);
            DrawBone(img, jointCollection[JointType.FootLeft], jointCollection[JointType.AnkleLeft], depthOrColor);
        }
        private void DrawBone(Image<Bgr, Byte> img, Joint jointFrom, Joint jointTo, int depthOrColor)
        {
            if (jointFrom.TrackingState == JointTrackingState.NotTracked ||
            jointTo.TrackingState == JointTrackingState.NotTracked)
            {
                return; // nothing to draw, one of the joints is not tracked
            }

            if (jointFrom.TrackingState == JointTrackingState.Inferred ||
            jointTo.TrackingState == JointTrackingState.Inferred)
            {
                DrawBoneLine(img, jointFrom.Position, jointTo.Position, 1, depthOrColor);
            }

            if (jointFrom.TrackingState == JointTrackingState.Tracked &&
            jointTo.TrackingState == JointTrackingState.Tracked)
            {
                DrawBoneLine(img, jointFrom.Position, jointTo.Position, 3, depthOrColor);
            }
        }
        private void DrawBoneLine(Image<Bgr, Byte> img, SkeletonPoint p1, SkeletonPoint p2, int lineWidth, int depthOrColor)
        {
            System.Drawing.Point p_1, p_2;

            //depthOrColor = 0;
            if (depthOrColor == 0)
            {
                p_1 = SkeletonPointToDepthScreen(p1);
                p_2 = SkeletonPointToDepthScreen(p2);
            }
            else
            {
                p_1 = SkeletonPointToColorScreen(p1);
                p_2 = SkeletonPointToColorScreen(p2);
            }

            img.Draw(new LineSegment2D(p_1, p_2), new Bgr(255, 255, 0), lineWidth);
            img.Draw(new CircleF(p_1, 5), new Bgr(0, 0, 255), -1);

            StringBuilder str = new StringBuilder();
            str.AppendFormat("({0},{1},{2})", p1.X.ToString("0.0"), p1.Y.ToString("0.0"), p1.Z.ToString("0.0"));

            img.Draw(str.ToString(), ref font, p_1, new Bgr(0, 255, 0));
            img.Draw(new CircleF(p_2, 5), new Bgr(0, 0, 255), -1);

            str.Clear();
            str.AppendFormat("({0},{1},{2})", p2.X.ToString("0.0"), p2.Y.ToString("0.0"), p2.Z.ToString("0.0"));
            img.Draw(str.ToString(), ref font, p_2, new Bgr(0, 255, 0));
        }
        private System.Drawing.Point SkeletonPointToColorScreen(SkeletonPoint skelpoint)
        {
            ColorImagePoint colorPoint = this.sensor.CoordinateMapper.MapSkeletonPointToColorPoint(skelpoint, colorImageFormat);
            return new System.Drawing.Point(colorPoint.X, colorPoint.Y);
        }
        private System.Drawing.Point SkeletonPointToDepthScreen(SkeletonPoint skelpoint)
        {
            DepthImagePoint depthPoint = this.sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skelpoint, depthImageFormat);
            return new System.Drawing.Point(depthPoint.X, depthPoint.Y);
        }

        private void DatabaseOp_Click(object sender, EventArgs e)
        {

            foreach (Skeleton skeleton in this.skeletonData)
            {
                if (skeleton == null) continue;
                if (skeleton.TrackingState == SkeletonTrackingState.Tracked&&textBox1.Text!=null)
                {
                    Console.Write(" 头部 x " + skeleton.Joints[JointType.Head].Position.X);
                    Console.Write(" y " + skeleton.Joints[JointType.Head].Position.Y);
                    Console.WriteLine(" z " + skeleton.Joints[JointType.Head].Position.Z);
                    Console.Write(" 左手 x " + skeleton.Joints[JointType.HandLeft].Position.X);
                    Console.Write(" y " + skeleton.Joints[JointType.HandLeft].Position.Y);
                    Console.WriteLine(" z " + skeleton.Joints[JointType.HandLeft].Position.Z);
                    Console.Write(" 脊柱 x " + skeleton.Joints[JointType.Spine].Position.X);
                    Console.Write(" y " + skeleton.Joints[JointType.Spine].Position.Y);
                    Console.WriteLine(" z " + skeleton.Joints[JointType.Spine].Position.Z);
                    Console.WriteLine();
                    using (SqlConnection conn = new SqlConnection(connsql))
                    {
                        conn.Open();//打开数据库

                        SqlCommand cmd = conn.CreateCommand();
                        //创建查询语句
                        //cmd.CommandText = "SELECT * FROM pos1";
                        ////从数据库中读取数据流存入reader中
                        //SqlDataReader reader = cmd.ExecuteReader();
                        //Console.WriteLine(conn.State);
                        //while (reader.Read())
                        //{
                        //    string name = reader.GetString(reader.GetOrdinal("x1"));
                        //    //int age = reader.GetInt32(reader.GetOrdinal("age"));
                        //    Console.WriteLine(name);
                        //}

                        //reader.Close();//报bug，必须要关掉才可以执行。
                        String insert = "insert into positions values("+ skeleton.Joints[JointType.Head].Position.X+ ","+ skeleton.Joints[JointType.Head].Position.Y + ","+ skeleton.Joints[JointType.Head].Position.Z+ "," + textBox1.Text + ",'Head')";
                        String insert1 = " insert into positions values(" + skeleton.Joints[JointType.HandLeft].Position.X + "," + skeleton.Joints[JointType.HandLeft].Position.Y + "," + skeleton.Joints[JointType.HandLeft].Position.Z + ","+textBox1.Text+",'HandLeft')";
                        String insert2 = " insert into positions values(" + skeleton.Joints[JointType.WristLeft].Position.X + "," + skeleton.Joints[JointType.WristLeft].Position.Y + "," + skeleton.Joints[JointType.WristLeft].Position.Z + "," + textBox1.Text + ",'WristLeft')";
                        String insert3 = " insert into positions values(" + skeleton.Joints[JointType.ElbowLeft].Position.X + "," + skeleton.Joints[JointType.ElbowLeft].Position.Y + "," + skeleton.Joints[JointType.ElbowLeft].Position.Z + "," + textBox1.Text + ",'ElbowLeft')";
                        String insert4 = " insert into positions values(" + skeleton.Joints[JointType.ShoulderLeft].Position.X + "," + skeleton.Joints[JointType.ShoulderLeft].Position.Y + "," + skeleton.Joints[JointType.ShoulderLeft].Position.Z + "," + textBox1.Text + ",'ShoulderLeft')";
                        String insert5 = " insert into positions values(" + skeleton.Joints[JointType.ShoulderCenter].Position.X + "," + skeleton.Joints[JointType.ShoulderCenter].Position.Y + "," + skeleton.Joints[JointType.ShoulderCenter].Position.Z + "," + textBox1.Text + ",'ShoulderCenter')";
                        String insert6 = " insert into positions values(" + skeleton.Joints[JointType.ShoulderRight].Position.X + "," + skeleton.Joints[JointType.ShoulderRight].Position.Y + "," + skeleton.Joints[JointType.ShoulderRight].Position.Z + "," + textBox1.Text + ",'ShoulderRight')";
                        String insert7 = " insert into positions values(" + skeleton.Joints[JointType.ElbowRight].Position.X + "," + skeleton.Joints[JointType.ElbowRight].Position.Y + "," + skeleton.Joints[JointType.ElbowRight].Position.Z + "," + textBox1.Text + ",'ElbowRight')";
                        String insert8 = " insert into positions values(" + skeleton.Joints[JointType.WristRight].Position.X + "," + skeleton.Joints[JointType.WristRight].Position.Y + "," + skeleton.Joints[JointType.WristRight].Position.Z + "," + textBox1.Text + ",'WristRight')";
                        String insert9 = " insert into positions values(" + skeleton.Joints[JointType.HandRight].Position.X + "," + skeleton.Joints[JointType.HandRight].Position.Y + "," + skeleton.Joints[JointType.HandRight].Position.Z + "," + textBox1.Text + ",'HandRight')";
                        String insert10 = " insert into positions values(" + skeleton.Joints[JointType.FootLeft].Position.X + "," + skeleton.Joints[JointType.FootLeft].Position.Y + "," + skeleton.Joints[JointType.FootLeft].Position.Z + "," + textBox1.Text + ",'FootLeft')";
                        String insert11 = " insert into positions values(" + skeleton.Joints[JointType.AnkleLeft].Position.X + "," + skeleton.Joints[JointType.AnkleLeft].Position.Y + "," + skeleton.Joints[JointType.AnkleLeft].Position.Z + "," + textBox1.Text + ",'AnkleLeft')";
                        String insert12 = " insert into positions values(" + skeleton.Joints[JointType.KneeLeft].Position.X + "," + skeleton.Joints[JointType.KneeLeft].Position.Y + "," + skeleton.Joints[JointType.KneeLeft].Position.Z + "," + textBox1.Text + ",'KneeLeft')";
                        String insert13 = " insert into positions values(" + skeleton.Joints[JointType.HipLeft].Position.X + "," + skeleton.Joints[JointType.HipLeft].Position.Y + "," + skeleton.Joints[JointType.HipLeft].Position.Z + "," + textBox1.Text + ",'HipLeft')";
                        String insert14 = " insert into positions values(" + skeleton.Joints[JointType.HipCenter].Position.X + "," + skeleton.Joints[JointType.HipCenter].Position.Y + "," + skeleton.Joints[JointType.HipCenter].Position.Z + "," + textBox1.Text + ",'HipCenter')";
                        String insert15 = " insert into positions values(" + skeleton.Joints[JointType.Spine].Position.X + "," + skeleton.Joints[JointType.Spine].Position.Y + "," + skeleton.Joints[JointType.Spine].Position.Z + "," + textBox1.Text + ",'Spine')";
                        String insert16 = " insert into positions values(" + skeleton.Joints[JointType.HipRight].Position.X + "," + skeleton.Joints[JointType.HipRight].Position.Y + "," + skeleton.Joints[JointType.HipRight].Position.Z + "," + textBox1.Text + ",'HipRight')";
                        String insert17 = " insert into positions values(" + skeleton.Joints[JointType.KneeRight].Position.X + "," + skeleton.Joints[JointType.KneeRight].Position.Y + "," + skeleton.Joints[JointType.KneeRight].Position.Z + "," + textBox1.Text + ",'KneeRight')";
                        String insert18 = " insert into positions values(" + skeleton.Joints[JointType.AnkleRight].Position.X + "," + skeleton.Joints[JointType.AnkleRight].Position.Y + "," + skeleton.Joints[JointType.AnkleRight].Position.Z + "," + textBox1.Text + ",'AnkleRight')";
                        String insert19 = " insert into positions values(" + skeleton.Joints[JointType.FootRight].Position.X + "," + skeleton.Joints[JointType.FootRight].Position.Y + "," + skeleton.Joints[JointType.FootRight].Position.Z + "," + textBox1.Text + ",'FootRight')";
                        String All = insert + insert1 + insert2 + insert3 + insert4 + insert5 + insert6 + insert7 + insert8 + insert9 + 
                            insert10 + insert11 + insert12 + insert13 + insert14 + insert15 + insert16 + insert17 + insert18 + insert19;
                        cmd.CommandText = All;
                        cmd.ExecuteNonQuery();

                    }
                }
            }

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void PictureBox1_Click(object sender, EventArgs e)
        {

        }
    }
}

