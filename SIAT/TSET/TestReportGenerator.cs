using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace SIAT.TSET
{
    /// <summary>
    /// 测试报告生成器 - 生成详细的测试执行报告
    /// </summary>
    public class TestReportGenerator
    {
        /// <summary>
        /// 生成详细测试报告
        /// </summary>
        public static string GenerateDetailedReport(TestCaseConfig testCase, List<TestStepResult> stepResults, 
            string barcode, TimeSpan totalDuration, bool testPassed)
        {
            var report = new StringBuilder();
            
            // 报告头部
            report.AppendLine("========================================");
            report.AppendLine("           测试执行报告");
            report.AppendLine("========================================");
            report.AppendLine();
            
            // 测试基本信息
            report.AppendLine("测试基本信息:");
            report.AppendLine($"  测试用例: {testCase.Name}");
            report.AppendLine($"  产品条码: {barcode}");
            report.AppendLine($"  执行时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"  总耗时: {totalDuration.TotalSeconds:F3} 秒");
            report.AppendLine($"  测试结果: {(testPassed ? "通过" : "失败")}");
            report.AppendLine();
            
            // 项目统计
            int totalSteps = stepResults.Count;
            int passedSteps = stepResults.Count(r => r.IsSuccess);
            int failedSteps = stepResults.Count(r => !r.IsSuccess);
            double passRate = totalSteps > 0 ? (double)passedSteps / totalSteps * 100 : 0;
            
            report.AppendLine("测试统计:");
            report.AppendLine($"  总步骤数: {totalSteps}");
            report.AppendLine($"  通过步骤: {passedSteps}");
            report.AppendLine($"  失败步骤: {failedSteps}");
            report.AppendLine($"  通过率: {passRate:F2}%");
            report.AppendLine();
            
            // 详细步骤结果
            report.AppendLine("详细步骤执行结果:");
            report.AppendLine("========================================");
            
            for (int i = 0; i < stepResults.Count; i++)
            {
                var result = stepResults[i];
                report.AppendLine($"步骤 {i + 1}: {result.StepName}");
                report.AppendLine($"  状态: {(result.IsSuccess ? "✓ 通过" : "✗ 失败")}");
                report.AppendLine($"  耗时: {result.Duration.TotalSeconds:F3} 秒");
                
                if (!string.IsNullOrEmpty(result.ActualValue))
                {
                    report.AppendLine($"  实际值: {result.ActualValue}");
                }
                
                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    report.AppendLine($"  错误信息: {result.ErrorMessage}");
                }
                
                report.AppendLine();
            }
            
            // 失败步骤汇总
            var failedResults = stepResults.Where(r => !r.IsSuccess).ToList();
            if (failedResults.Any())
            {
                report.AppendLine("失败步骤汇总:");
                report.AppendLine("========================================");
                
                foreach (var failedResult in failedResults)
                {
                    report.AppendLine($"• {failedResult.StepName}");
                    if (!string.IsNullOrEmpty(failedResult.ErrorMessage))
                    {
                        report.AppendLine($"  错误: {failedResult.ErrorMessage}");
                    }
                }
                report.AppendLine();
            }
            
            // 测试结论
            report.AppendLine("测试结论:");
            report.AppendLine("========================================");
            if (testPassed)
            {
                report.AppendLine("✓ 测试通过 - 所有测试步骤均执行成功");
            }
            else
            {
                report.AppendLine("✗ 测试失败 - 存在失败的测试步骤");
                report.AppendLine($"  失败步骤数量: {failedSteps}");
            }
            
            report.AppendLine();
            report.AppendLine("报告生成时间: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            
            return report.ToString();
        }
        
        /// <summary>
        /// 生成HTML格式的测试报告
        /// </summary>
        public static string GenerateHtmlReport(TestCaseConfig testCase, List<TestStepResult> stepResults, 
            string barcode, TimeSpan totalDuration, bool testPassed)
        {
            var html = new StringBuilder();
            
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html lang=\"zh-CN\">");
            html.AppendLine("<head>");
            html.AppendLine("    <meta charset=\"UTF-8\">");
            html.AppendLine("    <title>测试执行报告</title>");
            html.AppendLine("    <style>");
            html.AppendLine("        body { font-family: Arial, sans-serif; margin: 20px; }");
            html.AppendLine("        .header { background: #f5f5f5; padding: 20px; border-radius: 5px; margin-bottom: 20px; }");
            html.AppendLine("        .summary { background: #e8f4f8; padding: 15px; border-radius: 5px; margin-bottom: 20px; }");
            html.AppendLine("        .step-result { margin: 10px 0; padding: 10px; border-left: 4px solid #4CAF50; background: #f9f9f9; }");
            html.AppendLine("        .step-failed { border-left-color: #f44336; background: #ffeaea; }");
            html.AppendLine("        .conclusion { padding: 15px; border-radius: 5px; margin-top: 20px; }");
            html.AppendLine("        .passed { background: #e8f5e8; border: 1px solid #4CAF50; }");
            html.AppendLine("        .failed { background: #ffeaea; border: 1px solid #f44336; }");
            html.AppendLine("    </style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            
            // 报告头部
            html.AppendLine("    <div class=\"header\">");
            html.AppendLine("        <h1>测试执行报告</h1>");
            html.AppendLine("    </div>");
            
            // 测试基本信息
            html.AppendLine("    <div class=\"summary\">");
            html.AppendLine("        <h2>测试基本信息</h2>");
            html.AppendLine($"        <p><strong>测试用例:</strong> {testCase.Name}</p>");
            html.AppendLine($"        <p><strong>产品条码:</strong> {barcode}</p>");
            html.AppendLine($"        <p><strong>执行时间:</strong> {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");
            html.AppendLine($"        <p><strong>总耗时:</strong> {totalDuration.TotalSeconds:F3} 秒</p>");
            html.AppendLine($"        <p><strong>测试结果:</strong> <span style=\"color: {(testPassed ? "#4CAF50" : "#f44336")}\">{(testPassed ? "通过" : "失败")}</span></p>");
            html.AppendLine("    </div>");
            
            // 测试统计
            int totalSteps = stepResults.Count;
            int passedSteps = stepResults.Count(r => r.IsSuccess);
            int failedSteps = stepResults.Count(r => !r.IsSuccess);
            double passRate = totalSteps > 0 ? (double)passedSteps / totalSteps * 100 : 0;
            
            html.AppendLine("    <div class=\"summary\">");
            html.AppendLine("        <h2>测试统计</h2>");
            html.AppendLine($"        <p><strong>总步骤数:</strong> {totalSteps}</p>");
            html.AppendLine($"        <p><strong>通过步骤:</strong> <span style=\"color: #4CAF50\">{passedSteps}</span></p>");
            html.AppendLine($"        <p><strong>失败步骤:</strong> <span style=\"color: #f44336\">{failedSteps}</span></p>");
            html.AppendLine($"        <p><strong>通过率:</strong> {passRate:F2}%</p>");
            html.AppendLine("    </div>");
            
            // 详细步骤结果
            html.AppendLine("    <h2>详细步骤执行结果</h2>");
            for (int i = 0; i < stepResults.Count; i++)
            {
                var result = stepResults[i];
                string stepClass = result.IsSuccess ? "step-result" : "step-result step-failed";
                
                html.AppendLine($"    <div class=\"{stepClass}\">");
                html.AppendLine($"        <h3>步骤 {i + 1}: {result.StepName}</h3>");
                html.AppendLine($"        <p><strong>状态:</strong> <span style=\"color: {(result.IsSuccess ? "#4CAF50" : "#f44336")}\">{(result.IsSuccess ? "✓ 通过" : "✗ 失败")}</span></p>");
                html.AppendLine($"        <p><strong>耗时:</strong> {result.Duration.TotalSeconds:F3} 秒</p>");
                
                if (!string.IsNullOrEmpty(result.ActualValue))
                {
                    html.AppendLine($"        <p><strong>实际值:</strong> {result.ActualValue}</p>");
                }
                
                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    html.AppendLine($"        <p><strong>错误信息:</strong> {result.ErrorMessage}</p>");
                }
                
                html.AppendLine("    </div>");
            }
            
            // 测试结论
            string conclusionClass = testPassed ? "conclusion passed" : "conclusion failed";
            html.AppendLine($"    <div class=\"{conclusionClass}\">");
            html.AppendLine("        <h2>测试结论</h2>");
            if (testPassed)
            {
                html.AppendLine("        <p>✓ 测试通过 - 所有测试步骤均执行成功</p>");
            }
            else
            {
                html.AppendLine("        <p>✗ 测试失败 - 存在失败的测试步骤</p>");
                html.AppendLine($"        <p><strong>失败步骤数量:</strong> {failedSteps}</p>");
            }
            html.AppendLine("    </div>");
            
            html.AppendLine($"    <p style=\"text-align: center; margin-top: 30px; color: #666;\">报告生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");
            html.AppendLine("</body>");
            html.AppendLine("</html>");
            
            return html.ToString();
        }
        
        /// <summary>
        /// 保存报告到文件
        /// </summary>
        public static void SaveReportToFile(string reportContent, string barcode, string reportType = "txt")
        {
            try
            {
                // 创建报告目录
                string reportDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestReports");
                if (!Directory.Exists(reportDir))
                {
                    Directory.CreateDirectory(reportDir);
                }
                
                // 生成文件名
                string extension = reportType.ToLower() == "html" ? ".html" : ".txt";
                string fileName = $"{barcode}_{DateTime.Now:yyyyMMdd_HHmmss}{extension}";
                string filePath = Path.Combine(reportDir, fileName);
                
                // 写入报告文件
                File.WriteAllText(filePath, reportContent, Encoding.UTF8);
                
                // 记录保存成功
                System.Diagnostics.Debug.WriteLine($"测试报告已保存: {filePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存测试报告失败: {ex.Message}");
            }
        }
    }
}