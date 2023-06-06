using Microsoft.Reporting.WinForms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kalkulasi_jarak
{
    public partial class formbaru : Form
    {
        public formbaru(List<RawData> raws)
        {
            InitializeComponent();
            ReportDataSource rds = new ReportDataSource("DataSet1",raws.ToArray());
            this.reportViewer1.LocalReport.DataSources.Add(rds);
            this.reportViewer1.LocalReport.Refresh();
        }

        private void formbaru_Load(object sender, EventArgs e)
        {

        }
    }
}
