package com.solera.drivertrip;

import com.aventstack.extentreports.ExtentTest;
import org.junit.*;
import org.openqa.selenium.*;
import org.openqa.selenium.edge.EdgeDriver;
import org.openqa.selenium.support.ui.*;

import java.time.Duration;
import java.util.List;

public class ManageDriverTest {

    private WebDriver driver;
    private WebDriverWait wait;
    private ExtentTest test;

    @Before
    public void setUp() {
        System.setProperty("webdriver.edge.driver",
                "C:\\Users\\RajnishKumar.Sing\\Downloads\\edgedriver_win64\\msedgedriver.exe");

        driver = new EdgeDriver();
        wait = new WebDriverWait(driver, Duration.ofSeconds(15));
        driver.manage().window().maximize();

        // Start Report
        ExtentReportManager.startReport("ManageDriverTest");
        test = ExtentReportManager.getTest();
        test.info("Browser launched and test started");
    }

    @Test
    public void testAddAndDeleteDriver() throws InterruptedException {
        try {
            driver.get("http://localhost:5173/login");
            test.info("Opened login page");

            // Login
            wait.until(ExpectedConditions.visibilityOfElementLocated(By.name("username"))).sendKeys("Sachit M");
            driver.findElement(By.name("password")).sendKeys("Sachit@123");
            driver.findElement(By.cssSelector("button[type='submit']")).click();
            test.info("Logged in as Manager");

            wait.until(ExpectedConditions.urlContains("/dashboard"));
            test.pass("Redirected to dashboard");

            // Go to Manage Drivers
            WebElement manageDriversCard = wait.until(ExpectedConditions.elementToBeClickable(
                    By.cssSelector("[data-testid='manageDrivers']")));
            manageDriversCard.click();
            test.info("Clicked on Manage Drivers card");

            wait.until(ExpectedConditions.urlContains("/drivers"));
            test.pass("Navigated to /drivers page");

            // Open modal and fill form
            wait.until(ExpectedConditions.elementToBeClickable(By.name("addDriverButton"))).click();
            test.info("Opened Add Driver modal");

            String driverName = "Test Driver " + System.currentTimeMillis();
            String phoneNumber = "9876543210";

            wait.until(ExpectedConditions.visibilityOfElementLocated(By.name("driverName"))).sendKeys(driverName);
            driver.findElement(By.name("phoneNumber")).sendKeys(phoneNumber);
            driver.findElement(By.name("submitDriver")).click();
            test.info("Submitted driver form with name: " + driverName);

            // Wait and verify
            Thread.sleep(3000);
            Assert.assertTrue("Driver not added", driver.getPageSource().contains(driverName));
            System.out.println("Driver successfully added and verified");
            test.pass("Driver successfully added and verified");

            // (Optional: Extract driver ID)
            List<WebElement> rows = driver.findElements(By.cssSelector("table tbody tr"));
            String driverId = "";
            for (WebElement row : rows) {
                if (row.getText().contains(driverName)) {
                    driverId = row.findElement(By.xpath("./td[1]")).getText();
                    break;
                }
            }
            if (!driverId.isEmpty()) {
                test.pass("Driver ID captured: " + driverId);
            } else {
                test.warning("Driver ID not found in table");
            }

        } catch (Exception e) {
            test.fail("Test failed due to exception: " + e.getMessage());
            throw e;
        }
    }

    @After
    public void tearDown() throws InterruptedException {
        Thread.sleep(1000);
        driver.quit();
        test.info("Browser closed");

        // End Report
        ExtentReportManager.endReport();
    }
}
