using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Security.AccessControl;

namespace Kalkulasi_jarak
{
    public partial class Form1 : Form
    {
        SerialPort serialPort = new SerialPort();
        List<Pelari> pelaris = new List<Pelari>();
        TimerPerlombaan timer = new TimerPerlombaan();
        List<RawData> rawDataAll = new List<RawData>();
        List<RawData> rawDataPertandingan = new List<RawData>();
        enum StatusRunning
        {
            None, 
            Bersedia,
            Berlangsung
        }
        private StatusRunning status = StatusRunning.None;
        public Form1()
        {
            InitializeComponent();
            serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceived);
            control.Overlays.Add(new GMapOverlay());
            control.Overlays.Add(new GMapOverlay());

            control.MapProvider = GMapProviders.OpenStreetMap;
            control.Dock = DockStyle.Fill;
            panel1.Controls.Add(control);
            control.MinZoom = 2;
            control.MaxZoom = 20;
            timer.Elapsed += new System.Timers.ElapsedEventHandler(timerElapsed);
            timer.Interval = 1000;
        }
        int total = 0;
        DateTime _startTimer;
        private void timerElapsed(object sender, ElapsedEventArgs e)
        {
            total++;
            double timeSpan = Math.Floor(DateTime.Now.Subtract(_startTimer).TotalSeconds);
            double waktu_tersisa = ((timer.waktuPertandingan * 60) - timeSpan);
            int jam_sisa = Convert.ToInt32(Math.Floor(waktu_tersisa / 3600));
            int menit_sisa = Convert.ToInt32(Math.Floor((waktu_tersisa - (jam_sisa * 3600)) / 60));
            int detik_sisa = Convert.ToInt32(waktu_tersisa - ((jam_sisa * 3600) + (menit_sisa * 60)));
            this.BeginInvoke(new MethodInvoker(delegate
            {
                label3.Text = $"{(jam_sisa.ToString().Length == 2 ? jam_sisa.ToString() : $"0{jam_sisa.ToString()}")}:{(menit_sisa.ToString().Length == 2 ? menit_sisa.ToString() : $"0{menit_sisa.ToString()}")}:{(detik_sisa.ToString().Length == 2 ? detik_sisa.ToString() : $"0{detik_sisa.ToString()}")}";
             }));

            if (total >= this.timer.waktuPertandingan * 60)
            {
                status = StatusRunning.None;
                timer.Stop();
            }
        }
        GMapControl control = new GMapControl();
        private void DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string datas = serialPort.ReadLine();
                bool isNoise = true;
                if (status == StatusRunning.None)
                {
                    return;
                }
                if (String.IsNullOrEmpty(datas))
                    return;

                string[] data = datas.Split(',');

                string lat, lng, sog, vbat, id;
                if (data.Length != 6)
                {
                    rawDataAll.Add(new RawData
                    {
                        raw = datas,
                        status= "Succes"
                    }) ;
                    return;
                }

                try
                {
                    Convert.ToDouble(data[2]);
                    Convert.ToDouble(data[1]);
                    Convert.ToDouble(data[3]);
                    rawDataAll.Add(new RawData
                    {
                        ID = data[0],
                        Lat = data[1],
                        Lng = data[2],
                        Sog = data[3],
                        Baterai = data[4]
                    });
                }
                catch (Exception ex)
                {
                    rawDataAll.Add(new RawData
                    {
                        raw = datas,
                        status = "Succes"
                    });
                    Console.WriteLine(ex.Message);
                    return;
                }
                var pelari = pelaris.FirstOrDefault(x => x.Id == data[0].Trim());

                if (pelari == null)
                {
                    return;
                }
              

                PointLatLng latLng = new PointLatLng(Convert.ToDouble(data[1]), Convert.ToDouble(data[2]));

                if (status == StatusRunning.Berlangsung)
                {
                    pelari._pointLatLng.Add(latLng);
                    pelari.increaseJarak(latLng);

                    var temp = mengurutkan(pelaris);

                    this.BeginInvoke(new MethodInvoker(delegate
                    {
                        textBox1.Lines = new string[]
                        {
                        $"{temp[0].Id} = 1 = {temp[0].Jarak} Meter",
                        $"{temp[1].Id} = 2 = {temp[1].Jarak} Meter"
                        };
                        if (pelari.Id == "MKRRMP011222")
                        {
                            control.Overlays[0].Markers.Clear();
                            control.Overlays[0].Markers.Add(new GMarkerGoogle(latLng, GMarkerGoogleType.red));
                        }
                        else if (pelari.Id == "MKRRMP021222")
                        {
                            control.Overlays[1].Markers.Clear();
                            control.Overlays[1].Markers.Add(new GMarkerGoogle(latLng, GMarkerGoogleType.blue));
                        }
                    }));
                }
                else if (status == StatusRunning.Bersedia)
                {
                    pelari._pointLatLng.Clear();
                    pelari._pointLatLng.Add(latLng);
                    this.BeginInvoke(new MethodInvoker(delegate
                    {
                        if (pelari.Id == "MKRRMP011222")
                        {
                            control.Overlays[0].Markers.Clear();
                            control.Overlays[0].Markers.Add(new GMarkerGoogle(latLng, GMarkerGoogleType.red));
                        }
                        else if (pelari.Id == "MKRRMP021222")
                        {
                            control.Overlays[1].Markers.Clear();
                            control.Overlays[1].Markers.Add(new GMarkerGoogle(latLng, GMarkerGoogleType.blue));
                        }
                    }));
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        private List<Pelari> mengurutkan(List<Pelari> list)
        {
            List<Pelari> initial = new List<Pelari>();
            var temp = list.OrderByDescending(x => x.Jarak);
            foreach ( Pelari p in temp )
            {
                initial.Add(p);
            }
            return initial;
        } 

        static double CalculateDistance(PointLatLng p1, PointLatLng p2)
        {
            double earthRadius = 6371; // Radius Bumi dalam kilometer

            // Mengubah latitude dan longitude dari derajat ke radian
            double lat1Rad = ToRadians(p1.Lat);
            double lon1Rad = ToRadians(p1.Lng);
            double lat2Rad = ToRadians(p2.Lat);
            double lon2Rad = ToRadians(p2.Lng);

            // Menghitung perbedaan latitude dan longitude
            double dLat = lat2Rad - lat1Rad;
            double dLon = lon2Rad - lon1Rad;

            // Menggunakan formula Haversine
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            double distance = earthRadius * c;

            return distance * 1000;
        }

        static double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            pelaris.Add(new Pelari
            {
                Id = "MKRRMP011222",
                Jarak = 0,
                posisi = 1,
            });

            pelaris.Add(new Pelari
            {
                Id = "MKRRMP021222",
                Jarak = 0,
                posisi = 1,
            });
        }
        class Pelari
        {
            public  string Id { get; set; }
            public List<PointLatLng> _pointLatLng = new List<PointLatLng>();
            public double Jarak { get; set; }
            public int posisi { get; set; }

            public void increaseJarak(PointLatLng point)
            {
                if (this._pointLatLng.Count > 1)
                {
                    this.Jarak += CalculateDistance(_pointLatLng[_pointLatLng.Count - 2], point);
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (button1.Text == "Konek")
            {
                button1.Text = "Putuskan";
                serialPort.PortName = comboBox1.Text;
                serialPort.BaudRate = 9600;
                serialPort.Open();
            }
            else if (button1.Text == "Putuskan")
            {
                button1.Text = "Konek";
                serialPort.Close();
            }
        }

        private void lblMkr1_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            comboBox1.Items.Clear();
            string[] port = SerialPort.GetPortNames();
            if (port.Length > 0)
                comboBox1.Items.AddRange(port);
        }

        private void button3_Click(object sender, EventArgs e)
        {
          
            if (button3.Text == "Start")
            {
                if (numericUpDown1.Value == 0)
                {
                    MessageBox.Show("Dilarang menggunakan bilangan 0", "Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                status = StatusRunning.Bersedia;
                button3.Text = "Mulai";
            }
            else if (button3.Text == "Mulai")
            {
                _startTimer = DateTime.Now;
                timer.waktuPertandingan = Convert.ToInt32(numericUpDown1.Value);
                timer.waktuPertandingan += 1;
                status = StatusRunning.Berlangsung;
                button3.Text = "Stop";
                timer.Start();
            }
            else if (button3.Text == "Stop")
            {
                status = StatusRunning.None;
                timer.Stop();
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            string halo = "0.23232323232323232323";
            Convert.ToDouble(halo);
        }
    }

}
