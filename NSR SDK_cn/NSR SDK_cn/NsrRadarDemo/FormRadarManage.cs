using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NsrRadarSdk;
using NsrRadarSdk.NsrTypes;

namespace NsrRadarDemo
{
    public partial class FormRadarManage : Form
    {
        NsrRadar _radarSelected;
        private ConcurrentDictionary<string, NsrRadar> _radars;

        public FormRadarManage()
        {
            InitializeComponent();
            
            dataGridView1.AutoGenerateColumns = false;
            _radars = new ConcurrentDictionary<string, NsrRadar>();
            NsrSdk.Instance.Init(9000, false);
            NsrSdk.Instance.Timeout = 3000;
            try
            {
                NsrSdk.Instance.StartReceiveBroadcast(RadarBroadcast);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            
            NsrSdk.Instance.TargetDetect += FormTestRadar_TargetDetect;
            NsrSdk.Instance.RadarOnlineStateChanged += _manager_RadarConnect;
            
            dataGridView1.AutoGenerateColumns = false;
            UpdateRadars();

        }

        private void RadarBroadcast(NsrRadar radar, ref RVS_PARAM_BROADCAST info)
        {
            if (radar.Ip == "192.168.61.123" && radar.Online == false)
            {
                try
                {
                    //radar.Connect();
                }
                catch (Exception)
                {

                }
            }
            if (_radars.ContainsKey(radar.Ip))
                return;

            _radars[radar.Ip] = radar;

            this.BeginInvoke(new Action(() =>
            {
                UpdateRadars();
            }));
        }

        /// <summary>
        /// refresh the radar list
        /// </summary>
        void UpdateRadars()
        {
            dataGridView1.DataSource = _radars.Values;
        }

        void _manager_RadarConnect(NsrRadar radar, bool online)
        {
            this.BeginInvoke(new Action(() =>
            {
                UpdateRadars();
            }));
        }

        /// <summary>
        /// format target info and append to the textbox
        /// </summary>
        /// <param name="radar"></param>
        void FormTestRadar_TargetDetect(NsrRadar radar, RVS_Target_List targetList)
        {

            StringBuilder sb = new StringBuilder(targetList.TargetNum*40);
            DateTime now = DateTime.Now;

            foreach (var item in targetList.Targets)
            {
                sb.AppendLine(string.Format("X=\t{0}\t, Y=\t{1}\t, Time\t{2}", item.X.ToString("F2"),
                    item.Y.ToString("F2"), now.ToString()));
            }

            textBox8.Invoke(new MethodInvoker(() =>
                {
                    if (textBox8.Lines.Length > 1000)
                        textBox8.Clear();
                    textBox8.AppendText(sb.ToString());
                }
            ));

        }

        /// <summary>
        /// set heart time
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (_radarSelected == null)
                {
                    MessageBox.Show("Please select alarm radar)");
                    return;
                }
                int nHeartTime = int.Parse(radar_HeartTime.Text);

                if (nHeartTime <= 0 || nHeartTime > 60)
                {

                    MessageBox.Show("HeartTime >0 &&HeartTime<60");
                    return;
                }
                if (_radarSelected.SetHeartTime((byte) nHeartTime))
                {
                    select();
                    MessageBox.Show("Set successfully");
                }
                else
                {
                    MessageBox.Show("Set failure)");
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.ToString());
                Log.Error(ex.ToString());
            }
        }

        /// <summary>
        /// set filter coordinate
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            if (_radarSelected == null)
            {
                MessageBox.Show("Please select alarm radar)");
                return;
            }
            PointF[] pts = new PointF[4];
            try
            {
                pts[0].X = float.Parse(qqTextBoxEx_radar_x1.Text);
                pts[0].Y = float.Parse(qqTextBoxEx_radar_y1.Text);
                pts[1].X = float.Parse(qqTextBoxEx_radar_x2.Text);
                pts[1].Y = float.Parse(qqTextBoxEx_radar_y2.Text);
                pts[2].X = float.Parse(qqTextBoxEx_radar_x3.Text);
                pts[2].Y = float.Parse(qqTextBoxEx_radar_y3.Text);
                pts[3].X = float.Parse(qqTextBoxEx_radar_x4.Text);
                pts[3].Y = float.Parse(qqTextBoxEx_radar_y4.Text);

                _radarSelected.SetCoordinate(pts);

                select();
                MessageBox.Show("Set successfully");
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.ToString());
                Log.Error(ex.ToString());
            }
        }

        /// <summary>
        /// change radar ip
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            NsrRadar radar = null;
            try
            {
                if (dataGridView1.CurrentRow != null)
                {
                    string ip = dataGridView1.CurrentRow.Cells["RadarIP"].Value.ToString();
                    radar = _radars[ip];
                }
                if (_radarSelected == null)
                {
                    MessageBox.Show("Please select alarm radar)");
                    return;
                }
                if (radar != null)
                {
                    IPAddress _ip = null;
                    IPAddress _netmask = null;
                    IPAddress _gateway = null;
                    try
                    {

                        _ip = IPAddress.Parse(text_ip.Text);
                        if (_ip.ToString() == radar.Ip)
                        {
                            return;
                        }
                        else if (_radars.ContainsKey(_ip.ToString()))
                        {
                            throw new ArgumentException("radar ip exist");
                        }
                        //    CheckLocalIp(_ip);
                    }
                    catch
                    {
                        MessageBox.Show("invalid ip");
                        return;
                    }
                    try
                    {
                        _netmask = IPAddress.Parse(text_netmask.Text);
                    }
                    catch
                    {
                        MessageBox.Show("invalid netmask");
                        return;
                    }
                    try
                    {
                        _gateway = IPAddress.Parse(text_gateway.Text);
                    }
                    catch
                    {
                        MessageBox.Show("invalid gateway");
                        return;
                    }
                    radar.SetIpAddress(_ip, _netmask, _gateway);

                    Thread.Sleep(1000);
                    bindingSource1.DataSource = _radars.Values;
                    UpdateRadars();
                }
                else
                {
                    
                }
            }
            catch (System.Exception ex)
            {
                Log.Error( ex.ToString());

            }
        }

        private void dataGridView1_CellMouseUp(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
            {
                if (e.RowIndex < 0)
                    return;

                textBox7.Clear();
                select();
                return;
            }

            dataGridView1.ClearSelection();
            var hit = e;
            if (hit.RowIndex >= 0)
            {
                dataGridView1.Rows[hit.RowIndex].Selected = true;
                if (hit.ColumnIndex >= 0)
                {
                    dataGridView1.CurrentCell = dataGridView1.Rows[hit.RowIndex].Cells[hit.ColumnIndex];
                }
                else
                {
                    dataGridView1.CurrentCell = dataGridView1.Rows[hit.RowIndex].Cells[0];
                }

            }
            
        }

        /// <summary>
        /// query radar info
        /// </summary>
        void select()
        {
            try
            {
                if (dataGridView1.CurrentRow != null)
                {
                    string ip = dataGridView1.CurrentRow.Cells["RadarIP"].Value.ToString();
                    NsrRadar radar = _radars[ip];
                    radar.Connect();
                    _radarSelected = radar;
                    if (radar != null)
                    {
                        rvs_PARAM_STATUS state = new rvs_PARAM_STATUS();
                        if (radar.GetStatus(ref state))
                        {
                            textBox1.Text = "0x" + state.addr.ToString("x2");
                            textBox2.Text = state.heart.time.ToString();
                            textBox3.Text = state.bee.IsOpen.ToString();
                            textBox4.Text = state.radarVerInfo.FirmwareVersion;
                            textBox5.Text = state.radarVerInfo.AlgorithmVersion;
                            textBox6.Text = state.radarVerInfo.FpgaVersion;
                            textBox7.Clear();
                            for (int i = 0; i < radar.PtsAlarmAreaVertices.Length; i++)
                            {
                                textBox7.AppendText(i.ToString("D2"));
                                textBox7.AppendText(" , ");
                                textBox7.AppendText(radar.PtsAlarmAreaVertices[i].ToString());
                                textBox7.AppendText("\r\n");
                            }

                        }
                        else
                        {
                            textBox1.Clear();
                            textBox2.Clear();
                            textBox3.Clear();
                            textBox4.Clear();
                            textBox5.Clear();
                            textBox6.Clear();
                            textBox7.Clear();
                            MessageBox.Show("Query failure)");
                        }

                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            textBox8.Text = "";
        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            try
            {
                IPAddress ip = IPAddress.Parse(textBox12.Text);
                int port = int.Parse(textBox10.Text);

                NsrRadar radar = NsrSdk.Instance.CreateRadar(ip.ToString(), port);
                radar.Connect();

                _radars[radar.Ip] = radar;
                UpdateRadars();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

        }

        private void buttonDisConnect_Click(object sender, EventArgs e)
        {
            try
            {
                IPAddress ip = IPAddress.Parse(textBox12.Text);
                int port = int.Parse(textBox10.Text);

                if (_radars.ContainsKey(ip.ToString()) == false)
                    return;

                NsrRadar radar = null;

                _radars.TryRemove(ip.ToString(), out radar);
                if (radar != null)
                    radar.DisConnect();
                UpdateRadars();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }



    }
}
