package com.solera.drivertrip;

import com.aventstack.extentreports.*;
import com.aventstack.extentreports.reporter.ExtentSparkReporter;

public class ExtentReportManager {

    private static ExtentReports extent;
    private static ExtentTest test;

    public static void startReport(String testName) {
        if (extent == null) {
            ExtentSparkReporter spark = new ExtentSparkReporter("TestReport.html");
            extent = new ExtentReports();
            extent.attachReporter(spark);
        }
        test = extent.createTest(testName);
    }

    public static ExtentTest getTest() {
        return test;
    }

    public static void endReport() {
        if (extent != null) {
            extent.flush();
        }
    }
}
