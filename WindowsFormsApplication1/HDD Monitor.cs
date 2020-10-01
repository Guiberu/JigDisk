using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Management;
using System.Management.Instrumentation;
using System.Collections.Specialized;
using System.Threading;



namespace HDDMonitor
{
    public partial class HDDMonitorCode : Form
    {
        #region Variables
        //Globally accessable variables
        NotifyIcon hddLedIcon;
        Icon activeIcon;
        Icon idleIcon;
        Thread hddLedWorker;
        #endregion

        #region Hiding Window + Menu Code

        public HDDMonitorCode()
        {
            String title = "JigDisk HDD LED v0.1.1";
            InitializeComponent();

            //Load in the icons from files, into their own objects(variables) 
            activeIcon = new Icon("HD_Busy.ico");
            idleIcon = new Icon("HD_Idle.ico");
            hddLedIcon = new NotifyIcon
            {
                Icon = idleIcon, //Set a default icon
                Visible = true, //Make sure it's visisble
                Text = title //Displays Speed in ToolTip
            }; //Creates the actual notification icon for Windows

            //Make a right-click menu for the icon
            MenuItem progNameMenuItem = new MenuItem(title); //First menu entry & text
            MenuItem quitMenuItem = new MenuItem("Quit"); //Second menu entry & text
            ContextMenu contextMenu = new ContextMenu(); //Creates the menu itself
            contextMenu.MenuItems.Add(progNameMenuItem); //Adds the item to the menu
            contextMenu.MenuItems.Add(quitMenuItem); //Adds the item to the menu

            hddLedIcon.ContextMenu = contextMenu; //Links the newly made menu, to the icon in the taskbar

            quitMenuItem.Click += QuitMenuItem_Click;  //Creates a function for clicking the Quit item
            progNameMenuItem.Click += ProgNameMenuItem_Click; //Creates a function for clicking the Program Name item

            //Need to get rid of the window, as this is application is only for the taskbar

            this.WindowState = FormWindowState.Minimized; //Get rid of the window
            this.ShowInTaskbar = false; //Get rid of the taskbar icon

            //Once above setup is complete, start the thread for monitoring HDD activity
            hddLedWorker = new Thread(new ThreadStart(HddActivityThread));
            hddLedWorker.Start();

        }

        //What happens when you click the Program Name item
        private void ProgNameMenuItem_Click(object sender, EventArgs e) 
        {            
            MessageBox.Show("Thanks for looking!", "Hello", MessageBoxButtons.OK, MessageBoxIcon.Hand);
        }

        //What happens when you click the quit menu button
        private void QuitMenuItem_Click(object sender, EventArgs e)
        {
            hddLedWorker.Abort();
            hddLedIcon.Dispose();        
            this.Close();
        }
        #endregion

        #region HDD Monitoring Thread Code

        //This is a separate thread for pulling HDD activity and updating notification icon
        public void HddActivityThread()
        {

            ManagementClass driveDataClass = new ManagementClass("Win32_PerfformattedData_PerfDisk_PhysicalDisk");
            UInt64 total_disk_read_size = 0;
            UInt64 add_disk_read_size = 0;

            try
            {
                //An infinite loop, for monitoring without end
                while (true)
                {
                    total_disk_read_size = 0;
                    ManagementObjectCollection driveDataClassCollection = driveDataClass.GetInstances(); //Access the information on disk activity through WMI
                    foreach( ManagementObject obj in driveDataClassCollection) 
                    {
                        if( obj["Name"].ToString() == "_Total") // Target the specific value we want to read (Total)
                        {
                            add_disk_read_size = Convert.ToUInt64(obj["DiskBytesPersec"]);
                            if (  add_disk_read_size > 0 ) //Pull, convert, and check the bytes being read/written by the disc
                            {
                                total_disk_read_size += add_disk_read_size;
                            }
                        }
                    }
                    if(total_disk_read_size > 0)
                    {
                        hddLedIcon.Icon = activeIcon;
                    } else {
                        hddLedIcon.Icon = idleIcon;
                    }
                    Thread.Sleep(1000/60); //millisecond delay between looping to minimzie CPU usage
                }
            } catch( ThreadAbortException tbe ) // Cleanup code for when the program is closed
            {
                driveDataClass.Dispose();
                //Thread aborted 
            }
        }

        #endregion


    }
    
}

//Jamie Carl Gibson 2016
