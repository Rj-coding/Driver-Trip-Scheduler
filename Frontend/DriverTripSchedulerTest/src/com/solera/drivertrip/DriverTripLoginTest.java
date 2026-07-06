package com.solera.drivertrip;

import com.aventstack.extentreports.ExtentTest;
import org.openqa.selenium.By;
import org.openqa.selenium.WebDriver;
import org.openqa.selenium.WebElement;
import org.openqa.selenium.edge.EdgeDriver;

public class DriverTripLoginTest {

    public static void main(String[] args) throws InterruptedException {

        // Set driver path
        System.setProperty("webdriver.edge.driver", 
            "C:\\Users\\RajnishKumar.Sing\\Downloads\\edgedriver_win64\\msedgedriver.exe");

        WebDriver driver = new EdgeDriver();

        // ✅ Start Spark Report
        ExtentReportManager.startReport("DriverTripLoginTest");
        ExtentTest test = ExtentReportManager.getTest();

        try {
            // Open login page
            driver.get("http://localhost:5173/login");
            driver.manage().window().maximize();
            test.info("Opened login page");
            Thread.sleep(1500);

            // Fill credentials
            WebElement usernameInput = driver.findElement(By.name("username"));
            WebElement passwordInput = driver.findElement(By.name("password"));
            WebElement loginButton = driver.findElement(By.cssSelector("button[type='submit']"));

            usernameInput.sendKeys("Sachit M");
            test.info("Entered username: Sachit M");

            Thread.sleep(1500);
            passwordInput.sendKeys("Sachit@123");
            test.info("Entered password");

            Thread.sleep(1500);
            loginButton.click();
            test.info("Clicked login button");

            // Wait for redirection
            Thread.sleep(3000);

            String currentUrl = driver.getCurrentUrl();
            if (currentUrl.contains("/dashboard")) {
            	System.out.println("Login successful. Redirected to Dashboard");
            	test.pass("✅ Login successful. Redirected to Dashboard.");
            } else {
            	System.out.println("Login failed or redirection incorrect. URL: " + currentUrl);
                test.fail("❌ Login failed or redirection incorrect. URL: " + currentUrl);
            }

        } catch (Exception e) {
            e.printStackTrace();
            System.out.println("Exception occured");
            test.fail("❌ Exception occurred: " + e.getMessage());
        } finally {
            Thread.sleep(2000);
            driver.quit();
            test.info("Browser closed");

            //  End report
            ExtentReportManager.endReport();
        }
    }
}
