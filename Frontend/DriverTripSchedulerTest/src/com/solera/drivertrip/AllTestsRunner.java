package com.solera.drivertrip;

import org.junit.runner.JUnitCore;

public class AllTestsRunner {
    public static void main(String[] args) throws Exception {
        JUnitCore.runClasses(ManageDriverTest.class);
        JUnitCore.runClasses(ManageVehiclesTest.class);

        DriverTripLoginTest.main(null);
        AssignTripTest.main(null);

        //  Only ONE flush here
        ExtentReportManager.endReport();
    }
}
