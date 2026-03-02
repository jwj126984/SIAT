using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using SIAT.ResourceManagement;

namespace SIAT.TSET
{
    public partial class TestSettingsWindow : Window
    {
        public TestSettings Settings { get; private set; }
        private DispatcherTimer _portScanTimer;

        public TestSettingsWindow(TestSettings currentSettings)
        {
            // 先初始化Settings对象，避免InitializeComponent()中触发事件时出现空引用
            Settings = currentSettings ?? new TestSettings();
            
            InitializeComponent();
            
            // 初始化界面
            InitializeUI();
            
            // 初始化端口扫描定时器
            _portScanTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _portScanTimer.Tick += PortScanTimer_Tick;
            _portScanTimer.Start();
            
            // 初始扫描端口
            UpdatePortList();
        }

        private void InitializeUI()
        {
            // 设置启动方式
            switch (Settings.StartMode)
            {
                case StartMode.Software:
                    SoftwareStartRadio.IsChecked = true;
                    break;
                case StartMode.Barcode:
                    BarcodeStartRadio.IsChecked = true;
                    break;
                case StartMode.Tooling:
                    ToolingStartRadio.IsChecked = true;
                    break;
            }
            
            // 设置条码长度
            BarcodeLengthTextBox.Text = Settings.BarcodeLength.ToString();
            
            // 设置工装板参数
            // 首先设置文本，确保显示正确
            ToolingPortComboBox.Text = Settings.ToolingPort;
            // 尝试设置SelectedItem，如果端口存在于列表中
            if (!string.IsNullOrEmpty(Settings.ToolingPort))
            {
                ToolingPortComboBox.SelectedItem = Settings.ToolingPort;
            }
            ToolingBaudRateComboBox.Text = Settings.ToolingBaudRate.ToString();
            
            // 加载CAN设备列表
            LoadCanDevices();
            
            // 设置CAN设备
            if (!string.IsNullOrEmpty(Settings.CanDeviceName))
            {
                CanDeviceComboBox.SelectedItem = Settings.CanDeviceName;
            }
            
            // 更新CAN设备设置面板的可见性
            UpdateCanDeviceSettingsVisibility();
        }
        
        /// <summary>
        /// 加载CAN设备列表
        /// </summary>
        private void LoadCanDevices()
        {
            CanDeviceComboBox.Items.Clear();
            
            // 读取设备文件夹
            string devicesFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Devices");
            if (Directory.Exists(devicesFolderPath))
            {
                string[] deviceFiles = Directory.GetFiles(devicesFolderPath, "*.xml");
                foreach (string file in deviceFiles)
                {
                    try
                    {
                        Device device = XmlHelper.DeserializeFromFile<Device>(file);
                        // 只添加CAN类型且已启用的设备
                        if (device.CommunicationType == CommunicationType.CAN && device.IsEnabled && !string.IsNullOrEmpty(device.Name))
                        {
                            CanDeviceComboBox.Items.Add(device.Name);
                        }
                    }
                    catch (Exception ex)
                    {
                        // 忽略无效的设备文件
                    }
                }
            }
        }

        private void StartModeChanged(object sender, RoutedEventArgs e)
        {
            // 根据选择的启动方式更新设置
            if (SoftwareStartRadio.IsChecked == true)
            {
                Settings.StartMode = StartMode.Software;
            }
            else if (BarcodeStartRadio.IsChecked == true)
            {
                Settings.StartMode = StartMode.Barcode;
            }
            else if (ToolingStartRadio.IsChecked == true)
            {
                Settings.StartMode = StartMode.Tooling;
            }
            
            // 更新CAN设备设置面板的可见性
            UpdateCanDeviceSettingsVisibility();
        }
        
        /// <summary>
        /// 更新CAN设备设置面板的可见性
        /// </summary>
        private void UpdateCanDeviceSettingsVisibility()
        {
            // 只在选择工装启动时显示CAN设备设置
            CanDeviceSettingsPanel.Visibility = ToolingStartRadio.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        private void PortScanTimer_Tick(object sender, EventArgs e)
        {
            UpdatePortList();
        }

        private void UpdatePortList()
        {
            try
            {
                string[] ports = SerialPort.GetPortNames();
                
                // 检查端口列表是否有变化
                bool hasChanges = false;
                if (ToolingPortComboBox.Items.Count != ports.Length)
                {
                    hasChanges = true;
                }
                else
                {
                    for (int i = 0; i < ports.Length; i++)
                    {
                        if (ToolingPortComboBox.Items[i].ToString() != ports[i])
                        {
                            hasChanges = true;
                            break;
                        }
                    }
                }
                
                if (hasChanges)
                {
                    // 保存当前设置的端口（优先使用Settings中的端口，其次是当前选中的端口）
                    string savedPort = !string.IsNullOrEmpty(Settings.ToolingPort) ? Settings.ToolingPort : ToolingPortComboBox.SelectedItem?.ToString();
                    
                    // 更新端口列表
                    ToolingPortComboBox.Items.Clear();
                    foreach (string port in ports)
                    {
                        ToolingPortComboBox.Items.Add(port);
                    }
                    
                    // 恢复选中的端口
                    if (!string.IsNullOrEmpty(savedPort) && Array.Exists(ports, p => p == savedPort))
                    {
                        ToolingPortComboBox.SelectedItem = savedPort;
                        ToolingPortComboBox.Text = savedPort;
                    }
                    else if (ports.Length > 0)
                    {
                        ToolingPortComboBox.SelectedIndex = 0;
                    }
                    else
                    {
                        // 如果没有可用端口，清空选择
                        ToolingPortComboBox.SelectedItem = null;
                        ToolingPortComboBox.Text = "";
                    }
                }
            }
            catch (Exception ex)
            {
                // 端口扫描失败，忽略
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 先根据界面选择更新启动方式
                if (SoftwareStartRadio.IsChecked == true)
                {
                    Settings.StartMode = StartMode.Software;
                }
                else if (BarcodeStartRadio.IsChecked == true)
                {
                    Settings.StartMode = StartMode.Barcode;
                }
                else if (ToolingStartRadio.IsChecked == true)
                {
                    Settings.StartMode = StartMode.Tooling;
                }
                
                // 保存条码长度
                if (int.TryParse(BarcodeLengthTextBox.Text, out int barcodeLength) && barcodeLength > 0)
                {
                    Settings.BarcodeLength = barcodeLength;
                }
                else
                {
                    MessageBox.Show("请输入有效的条码长度", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                // 保存工装板参数
                Settings.ToolingPort = ToolingPortComboBox.SelectedItem?.ToString() ?? string.Empty;
                
                if (int.TryParse(ToolingBaudRateComboBox.Text, out int baudRate))
                {
                    Settings.ToolingBaudRate = baudRate;
                }
                
                // 保存CAN设备参数
                Settings.CanDeviceName = CanDeviceComboBox.SelectedItem?.ToString() ?? string.Empty;
                
                // 保存设置
                Settings.Save();
                
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存设置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            
            // 停止端口扫描定时器
            _portScanTimer.Stop();
        }
    }
}
