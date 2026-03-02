using SIAT.ResourceManagement;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SIAT
{
    public partial class ResourceManagementWindow : Window
    {
        private readonly List<Device> devices;
        private Device? selectedDevice;
        private Step? selectedStep;
        private readonly string devicesFolderPath;

        public ResourceManagementWindow()
        {
            InitializeComponent();
            devices = [];
            selectedDevice = null;
            selectedStep = null;
            devicesFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Devices");
            LoadDevices();
        }

        private void LoadDevices()
        {
            devices.Clear();
            if (!Directory.Exists(devicesFolderPath))
            {
                Directory.CreateDirectory(devicesFolderPath);
            }

            string[] deviceFiles = Directory.GetFiles(devicesFolderPath, "*.xml");
            foreach (string file in deviceFiles)
            {
                try
                {
                    Device device = XmlHelper.DeserializeFromFile<Device>(file);
                    // 只有当设备名称不为空时才添加到列表中
                    if (!string.IsNullOrEmpty(device.Name))
                    {
                        devices.Add(device);
                    }
                }
                catch (Exception ex)
                {
                    // 记录日志或显示警告
                    MessageBox.Show($"加载设备文件 {Path.GetFileName(file)} 失败: {ex.Message}", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            DeviceListView.ItemsSource = devices;
        }

        private void SaveDevices()
        {
            foreach (Device device in devices)
            {
                string filePath = Path.Combine(devicesFolderPath, $"{device.Name}.xml");
                XmlHelper.SerializeToFile(device, filePath);
            }
        }

        private void DeviceListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DeviceListView.SelectedItem != null)
            {
                selectedDevice = (Device)DeviceListView.SelectedItem;
                selectedStep = null;
                StepListView.ItemsSource = selectedDevice.Steps;
                StepListView.SelectedItem = null;
                EditDeviceButton.IsEnabled = true;
                DeleteDeviceButton.IsEnabled = true;
                AddStepButton.IsEnabled = true;
                EditStepButton.IsEnabled = false;
                DeleteStepButton.IsEnabled = false;
                
                // 显示设备详细信息，隐藏步骤详细信息
                DeviceDetailPanel.Visibility = Visibility.Visible;
                StepDetailPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void DeviceListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (selectedDevice != null)
            {
                EditDevice();
                // 确保设备详细信息面板可见
                DeviceDetailPanel.Visibility = Visibility.Visible;
                StepDetailPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void DeviceListView_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // 当点击已经选中的设备时，确保显示设备详细信息
            if (selectedDevice != null && DeviceListView.SelectedItem == selectedDevice)
            {
                DeviceDetailPanel.Visibility = Visibility.Visible;
                StepDetailPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void AddDeviceButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddEditDeviceDialog(null);
            if (dialog.ShowDialog() == true)
            {
                Device newDevice = dialog.Device;
                devices.Add(newDevice);
                DeviceListView.Items.Refresh();
            }
        }

        private void EditDeviceButton_Click(object sender, RoutedEventArgs e)
        {
            EditDevice();
        }

        private void EditDevice()
        {
            if (selectedDevice != null)
            {
                var dialog = new AddEditDeviceDialog(selectedDevice);
                if (dialog.ShowDialog() == true)
                {
                    DeviceListView.Items.Refresh();
                }
            }
        }

        private void DeleteDeviceButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedDevice != null)
            {
                MessageBoxResult result = MessageBox.Show($"确定要删除设备 '{selectedDevice.Name}' 吗？", "删除确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    string filePath = Path.Combine(devicesFolderPath, $"{selectedDevice.Name}.xml");
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                    devices.Remove(selectedDevice);
                    DeviceListView.Items.Refresh();
                    StepListView.ItemsSource = null;
                    selectedDevice = null;
                    selectedStep = null;
                    EditDeviceButton.IsEnabled = false;
                    DeleteDeviceButton.IsEnabled = false;
                    AddStepButton.IsEnabled = false;
                    EditStepButton.IsEnabled = false;
                    DeleteStepButton.IsEnabled = false;
                    
                    // 隐藏所有详细信息面板
                    DeviceDetailPanel.Visibility = Visibility.Collapsed;
                    StepDetailPanel.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void AddStepButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedDevice != null)
            {
                var dialog = new AddEditStepDialog(null, selectedDevice);
                if (dialog.ShowDialog() == true)
                {
                    Step newStep = dialog.Step;
                    selectedDevice.Steps.Add(newStep);
                    StepListView.Items.Refresh();
                }
            }
        }

        private void EditStepButton_Click(object sender, RoutedEventArgs e)
        {
            EditStep();
        }

        private void StepListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StepListView.SelectedItem != null)
            {
                selectedStep = (Step)StepListView.SelectedItem;
                EditStepButton.IsEnabled = true;
                DeleteStepButton.IsEnabled = true;
                
                // 显示步骤详细信息，隐藏设备详细信息
                StepDetailPanel.DataContext = selectedStep;
                StepDetailPanel.Visibility = Visibility.Visible;
                DeviceDetailPanel.Visibility = Visibility.Collapsed;
                
                // 更新变量列表列的可见性
                UpdateResultVarListColumnsVisibility();
            }
        }
        
        private void UpdateResultVarListColumnsVisibility()
        {
            if (selectedStep == null || selectedStep.Protocol == null)
                return;
            
            // 获取变量列表的ListView
            if (FindName("ResultVariablesListView") is not ListView resultVarListView || resultVarListView.View == null)
                return;
            
            if (resultVarListView.View is not GridView gridView)
                return;
            
            // 存储原始宽度的字典
            var originalWidths = new Dictionary<string, double>
            {
                { "变量名称", 120 },
                { "单位", 60 },
                { "起始位", 100 },
                { "结束位", 100 },
                { "长度", 100 },
                { "分辨率", 100 },
                { "偏移量", 100 },
                { "字节序", 100 },
                { "CAN ID", 100 }
            };
            
            ProtocolType protocolType = selectedStep.Protocol.Type;
            
            // 设置列宽
            foreach (GridViewColumn column in gridView.Columns)
            {
                if (column.Header == null)
                    continue;
                
                string header = column.Header.ToString()!;
                
                // 始终显示基本信息列
                if (header == "变量名称" || 
                    header == "单位" || 
                    header == "起始位" ||
                    header == "分辨率" || 
                    header == "偏移量")
                {
                    column.Width = originalWidths[header];
                }
                // 根据协议类型显示特定列
                else if (header == "结束位")
                {
                    column.Width = (protocolType == ProtocolType.HEX || protocolType == ProtocolType.ASCII) ? originalWidths[header] : 0;
                }
                else if (header == "长度")
                {
                    column.Width = (protocolType == ProtocolType.CAN) ? originalWidths[header] : 0;
                }
                else if (header == "字节序")
                {
                    column.Width = (protocolType == ProtocolType.CAN) ? originalWidths[header] : 0;
                }
                else if (header == "CAN ID")
                {
                    column.Width = (protocolType == ProtocolType.CAN) ? originalWidths[header] : 0;
                }
            }
        }

        private void StepListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            EditStep();
        }

        private void StepListView_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // 当点击已经选中的步骤时，确保显示步骤详细信息
            if (selectedStep != null && StepListView.SelectedItem == selectedStep)
            {
                StepDetailPanel.DataContext = selectedStep;
                StepDetailPanel.Visibility = Visibility.Visible;
                DeviceDetailPanel.Visibility = Visibility.Collapsed;
                
                // 更新变量列表列的可见性
                UpdateResultVarListColumnsVisibility();
            }
        }

        private void EditStep()
        {
            if (StepListView.SelectedItem != null && selectedDevice != null)
            {
                Step selectedStep = (Step)StepListView.SelectedItem;
                var dialog = new AddEditStepDialog(selectedStep, selectedDevice);
                if (dialog.ShowDialog() == true)
                {
                    StepListView.Items.Refresh();
                }
            }
        }

        private void DeleteStepButton_Click(object sender, RoutedEventArgs e)
        {
            if (StepListView.SelectedItem != null && selectedDevice != null)
            {
                Step selectedStep = (Step)StepListView.SelectedItem;
                MessageBoxResult result = MessageBox.Show($"确定要删除步骤 '{selectedStep.Name}' 吗？", "删除确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    selectedDevice.Steps.Remove(selectedStep);
                    StepListView.Items.Refresh();
                    this.selectedStep = null;
                    EditStepButton.IsEnabled = false;
                    DeleteStepButton.IsEnabled = false;
                    
                    // 隐藏步骤详细信息面板，显示设备详细信息面板
                    StepDetailPanel.Visibility = Visibility.Collapsed;
                    if (selectedDevice != null)
                    {
                        DeviceDetailPanel.Visibility = Visibility.Visible;
                    }
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveDevices();
            MessageBox.Show("设备信息已保存", "保存成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}