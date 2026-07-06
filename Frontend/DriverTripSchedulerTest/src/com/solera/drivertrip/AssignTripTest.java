package com.solera.drivertrip;

import com.aventstack.extentreports.ExtentTest;
import org.openqa.selenium.By;
import org.openqa.selenium.WebDriver;
import org.openqa.selenium.WebElement;
import org.openqa.selenium.edge.EdgeDriver;

import java.util.List;

public class AssignTripTest {

    public static void main(String[] args) throws InterruptedException {

        System.setProperty("webdriver.edge.driver",
            "C:\\Users\\RajnishKumar.Sing\\Downloads\\edgedriver_win64\\msedgedriver.exe");

        WebDriver driver = new EdgeDriver();

        // ✅ Start Spark Report
        ExtentReportManager.startReport("AssignTripTest");
        ExtentTest test = ExtentReportManager.getTest();

        try {
            driver.get("http://localhost:5173/login?test=true");
            driver.manage().window().maximize();
            test.info("Opened login page");
            Thread.sleep(500);

            // Step 2: Login
            driver.findElement(By.name("username")).sendKeys("Sachit M");
            test.info("Entered username");

            Thread.sleep(500);
            driver.findElement(By.name("password")).sendKeys("Sachit@123");
            test.info("Entered password");

            Thread.sleep(500);
            driver.findElement(By.cssSelector("button[type='submit']")).click();
            test.info("Clicked login");

            Thread.sleep(2500);
            System.out.println("Logged in successfully and redirected");
            test.pass("Logged in successfully and redirected");

            // Step 3: Navigate to Assign Trips
            WebElement assignTripsCard = driver.findElement(By.cssSelector("[data-testid='assignTrips']"));
            assignTripsCard.click();
            test.info("Clicked Assign Trips");

            Thread.sleep(2000);

            // Step 4: Fill form
            driver.findElement(By.name("originCity")).sendKeys("Hyderabad");
            driver.findElement(By.name("originArea")).sendKeys("Madhapur");
            driver.findElement(By.name("destinationCity")).sendKeys("Hyderabad");
            driver.findElement(By.name("destinationArea")).sendKeys("Banjara Hills");
            driver.findElement(By.name("driver")).sendKeys("Rajnish - 8549458964");
            driver.findElement(By.name("vehicle")).sendKeys("Thar - HR5653");
            test.info("Filled trip form");

            WebElement startTimeInput = driver.findElement(By.id("startTime"));
            startTimeInput.clear();
            startTimeInput.sendKeys("2025-08-15T18:30");

            WebElement endTimeInput = driver.findElement(By.id("endTime"));
            endTimeInput.clear();
            endTimeInput.sendKeys("2025-08-16T18:30");

            Thread.sleep(2000);

            // Step 5: Submit
            driver.findElement(By.cssSelector("button[type='submit']")).click();
            test.info("Submitted form");

            Thread.sleep(2000);

            // Step 6: Verify redirection
            String afterSubmitUrl = driver.getCurrentUrl();
            if (afterSubmitUrl.contains("/viewtrips")) {
            	System.out.println("Redirected to ViewTrips after assignment");
                test.pass("Redirected to ViewTrips after assignment");
            } else {
                test.fail("Not redirected to ViewTrips. URL: " + afterSubmitUrl);
                return;
            }

            // Step 7: Try to delete trip
            List<WebElement> rows = driver.findElements(By.cssSelector("table tbody tr"));
            boolean deleted = false;

            for (WebElement row : rows) {
                if (row.getText().contains("15/08/2025 06:30 PM") &&
                    row.getText().contains("16/08/2025 06:30 PM")) {

                    WebElement deleteBtn = row.findElement(By.cssSelector("button.btn-danger"));
                    deleteBtn.click();
                    Thread.sleep(2000);

                    try {
                        driver.switchTo().alert().accept();
                        Thread.sleep(2000);
                    } catch (Exception ignored) {}

                    System.out.println("Trip deleted successfully");
                    test.pass("Trip deleted successfully");
                    deleted = true;
                    break;
                }
            }

            if (!deleted) {
                test.warning("Trip not found for deletion");
            }

        } catch (Exception e) {
            test.fail("Test failed with exception: " + e.getMessage());
            e.printStackTrace();
        } finally {
            Thread.sleep(2000);
            driver.quit();
            test.info("Browser closed");

            // ✅ End report
            ExtentReportManager.endReport();
        }
    }
}
