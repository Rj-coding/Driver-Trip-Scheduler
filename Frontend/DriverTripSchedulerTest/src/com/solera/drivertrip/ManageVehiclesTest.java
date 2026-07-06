package com.solera.drivertrip;

import com.aventstack.extentreports.ExtentTest;
import org.junit.*;
import org.openqa.selenium.*;
import org.openqa.selenium.edge.EdgeDriver;
import org.openqa.selenium.support.ui.*;

import java.time.Duration;
import java.util.List;

public class ManageVehiclesTest {

    private WebDriver driver;
    private WebDriverWait wait;
    private ExtentTest test;

    @Before
    public void setUp() {
        System.setProperty("webdriver.edge.driver", "C:\\Users\\RajnishKumar.Sing\\Downloads\\edgedriver_win64\\msedgedriver.exe");
        driver = new EdgeDriver();
        wait = new WebDriverWait(driver, Duration.ofSeconds(15));
        driver.manage().window().maximize();

        // ✅ Start Report
        ExtentReportManager.startReport("ManageVehiclesTest");
        test = ExtentReportManager.getTest();
        test.info("Launched Edge and maximized window");
    }

    @Test
    public void testAddAndDeleteVehicle() throws InterruptedException {
        try {
            driver.get("http://localhost:5173/login");
            test.info("Opened login page");

            // Step 1: Login
            wait.until(ExpectedConditions.visibilityOfElementLocated(By.name("username"))).sendKeys("Sachit M");
            Thread.sleep(1500);
            driver.findElement(By.name("password")).sendKeys("Sachit@123");
            Thread.sleep(1500);
            driver.findElement(By.cssSelector("button[type='submit']")).click();
            test.info("Submitted login form");

            wait.until(ExpectedConditions.urlContains("/dashboard"));
            test.pass("Login successful and navigated to dashboard");

            // Step 2: Navigate to Manage Vehicles
            WebElement manageVehiclesCard = wait.until(ExpectedConditions.elementToBeClickable(
                    By.cssSelector("[data-testid='manageVehicles']")));
            manageVehiclesCard.click();
            test.info("Clicked on Manage Vehicles card");

            wait.until(ExpectedConditions.urlContains("/vehicles"));
            Thread.sleep(1500);
            test.pass("Navigated to /vehicles page");

            // Step 3: Open Add Vehicle Modal
            wait.until(ExpectedConditions.elementToBeClickable(
                    By.cssSelector("[data-testid='add-vehicle-button']"))).click();
            test.info("Opened Add Vehicle modal");

            // Step 4: Fill Vehicle Details
            String vehicleNumber = "AUTO-HR" + System.currentTimeMillis();
            String vehicleType = "SUV";

            Thread.sleep(1500);
            wait.until(ExpectedConditions.visibilityOfElementLocated(By.name("vehicleNumber"))).sendKeys(vehicleNumber);
            driver.findElement(By.name("vehicleType")).sendKeys(vehicleType);
            Thread.sleep(1500);
            test.info("Filled vehicle form: " + vehicleNumber);

            // Step 5: Submit form
            driver.findElement(By.cssSelector("[data-testid='submit-vehicle-button']")).click();
            test.info("Submitted vehicle form");

            // Step 6: Wait and verify
            Thread.sleep(3000);
            Assert.assertTrue("Vehicle not added", driver.getPageSource().contains(vehicleNumber));
            System.out.println("Vehicle added successfully and verified in table");
            test.pass("Vehicle added successfully and verified in table");

            // Step 7: Get vehicle ID from table
            String vehicleId = "";
            List<WebElement> rows = driver.findElements(By.cssSelector("table tbody tr"));
            for (WebElement row : rows) {
                if (row.getText().contains(vehicleNumber)) {
                    vehicleId = row.findElement(By.xpath("td[1]")).getText();
                    break;
                }
            }

            if (!vehicleId.isEmpty()) {
                test.pass("Captured Vehicle ID: " + vehicleId);
            } else {
                test.warning("Could not extract Vehicle ID from table");
            }

            // Step 8: Delete Vehicle
            WebElement deleteBtn = driver.findElement(
                    By.cssSelector("[data-testid='delete-button-" + vehicleId + "']"));
            deleteBtn.click();
            Thread.sleep(1500);

            // Step 9: Confirm delete alert
            wait.until(ExpectedConditions.alertIsPresent());
            driver.switchTo().alert().accept();
            Thread.sleep(3000);

            Assert.assertFalse("Vehicle not deleted", driver.getPageSource().contains(vehicleNumber));
            System.out.println("Vehicle deleted successfully");
            test.pass("Vehicle deleted successfully");

        } catch (Exception e) {
            test.fail("❌ Exception during test: " + e.getMessage());
            throw e;
        }
    }

    @After
    public void tearDown() throws InterruptedException {
        Thread.sleep(1000);
        driver.quit();
        test.info("Browser closed");

        // ✅ End Report
        ExtentReportManager.endReport();
    }
}
