using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using practice_2.Models;

namespace practice_2.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly String connStr = ("Server=srv477.hstgr.io;Database=u591433413_solvela;User=u591433413_solvela;Password=Solvela_Bank123$");
        private readonly HttpClient _httpClient;
        private readonly string apiBaseUrl = "https://solvela.site/api/index.php";

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult GetStarted()
        {
            return View();
        }

        public IActionResult CardDetails()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SaveCardDetails(CardDetails cardDetails)
        {
            if (!ModelState.IsValid)
            {
                return View("CardDetails");
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(cardDetails.CardNumber, @"^\d{4} \d{4} \d{4} \d{4}$"))
            {
                ModelState.AddModelError("CardNumber", "Card number must be in the format 0000 0000 0000 0000.");
                return View("CardDetails");
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(cardDetails.CVV, @"^\d{3}$"))
            {
                ModelState.AddModelError("CVV", "CVV must be exactly 3 digits.");
                return View("CardDetails");
            }

            // First, check if the card exists before any API call or insertions
            // Search with the formatted card number
            int? existingCardId = null;
            using (var conn = new MySqlConnection(connStr))
            {
                conn.Open();
                using var checkCmd = new MySqlCommand("SELECT Id FROM carddetails WHERE CardNumber = @CardNumber", conn);
                checkCmd.Parameters.AddWithValue("@CardNumber", cardDetails.CardNumber);
                var result = checkCmd.ExecuteScalar();
                if (result != null)
                {
                    existingCardId = Convert.ToInt32(result);
                }
            }

            // If card already exists, redirect to PIN page with that card ID
            if (existingCardId.HasValue)
            {
                TempData["InfoMessage"] = "This card already exists in our system.";
                return RedirectToAction("PinPage", new { cardId = existingCardId.Value });
            }

            // Store the formatted card number
            string formattedCardNumber = cardDetails.CardNumber;

            // Try API only if card doesn't exist
            int newCardId = 0;
            bool apiSuccess = false;

            try
            {
                // Set initial balance to 1000
                cardDetails.CurrentBalance = 1000;
                cardDetails.SavingsBalance = 0;

                var json = JsonSerializer.Serialize(cardDetails);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{apiBaseUrl}?endpoint=card-details", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);

                    if (responseData != null && responseData.TryGetValue("id", out var idObj))
                    {
                        newCardId = Convert.ToInt32(idObj);
                        apiSuccess = true;
                        return RedirectToAction("PinPage", new { cardId = newCardId });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"API call failed: {ex.Message}");
                // Continue with database insertion
            }

            // Only insert directly into database if API failed and card doesn't already exist
            if (!apiSuccess)
            {
                using var conn = new MySqlConnection(connStr);
                conn.Open();

                // Use a transaction to ensure atomicity
                using var transaction = conn.BeginTransaction();

                try
                {
                    // Double-check card doesn't exist (in case another request inserted it meanwhile)
                    using var doubleCheckCmd = new MySqlCommand("SELECT COUNT(*) FROM carddetails WHERE CardNumber = @CardNumber", conn, transaction);
                    doubleCheckCmd.Parameters.AddWithValue("@CardNumber", formattedCardNumber);
                    var count = Convert.ToInt32(doubleCheckCmd.ExecuteScalar());

                    if (count > 0)
                    {
                        // Card now exists (race condition), get its ID
                        using var getIdCmd = new MySqlCommand("SELECT Id FROM carddetails WHERE CardNumber = @CardNumber", conn, transaction);
                        getIdCmd.Parameters.AddWithValue("@CardNumber", formattedCardNumber);
                        var foundId = Convert.ToInt32(getIdCmd.ExecuteScalar());
                        transaction.Commit();
                        return RedirectToAction("PinPage", new { cardId = foundId });
                    }

                    // Card still doesn't exist, insert it with spaces (formatted) and initial balance of 1000
                    using var insertCmd = new MySqlCommand(
                        "INSERT INTO carddetails (CardHolderName, Email, CardNumber, ExpiryDate, CVV, CurrentBalance, SavingsBalance) " +
                        "VALUES (@CardHolderName, @Email, @CardNumber, @ExpiryDate, @CVV, 1000, 0)", conn, transaction);

                    insertCmd.Parameters.AddWithValue("@CardHolderName", cardDetails.CardHolderName);
                    insertCmd.Parameters.AddWithValue("@Email", cardDetails.Email);
                    insertCmd.Parameters.AddWithValue("@CardNumber", formattedCardNumber); // Store with spaces
                    insertCmd.Parameters.AddWithValue("@ExpiryDate", cardDetails.ExpiryDate);
                    insertCmd.Parameters.AddWithValue("@CVV", cardDetails.CVV);
                    insertCmd.ExecuteNonQuery();

                    newCardId = Convert.ToInt32(insertCmd.LastInsertedId);
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    _logger.LogError($"Database insert failed: {ex.Message}");
                    ModelState.AddModelError("", "Failed to save card details. Please try again.");
                    return View("CardDetails");
                }
            }

            return RedirectToAction("PinPage", new { cardId = newCardId });
        }

        public IActionResult PinPage(int cardId)
        {
            ViewBag.CardId = cardId;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SavePin(int cardId, string pin)
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(pin, @"^\d{4}$"))
            {
                TempData["AlertMessage"] = "PIN must be exactly 4 digits.";
                return RedirectToAction("PinPage", new { cardId });
            }

            try
            {
                // Try to save using the API
                var payload = new { CardId = cardId, Pin = pin };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{apiBaseUrl}?endpoint=pin", content);

                if (response.IsSuccessStatusCode)
                {
                    // Create an initial account opened transaction with 1000 initial balance
                    var transPayload = new
                    {
                        CardId = cardId,
                        Amount = 1000,
                        TransactionType = "Account Setup",
                        TransactionDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        Description = "Account activated with initial balance of 1000"
                    };

                    var transJson = JsonSerializer.Serialize(transPayload);
                    var transContent = new StringContent(transJson, Encoding.UTF8, "application/json");

                    await _httpClient.PostAsync($"{apiBaseUrl}?endpoint=transactions", transContent);

                    return RedirectToAction("MainMenu", new { cardId });
                }
            }
            catch (Exception ex)
            {
                // Log the exception and fall back to direct database access
                _logger.LogError($"API call failed: {ex.Message}");
            }

            // Fall back to direct database access
            using var conn = new MySqlConnection(connStr);
            conn.Open();

            // Use transaction for data consistency
            using var transaction = conn.BeginTransaction();

            try
            {
                // Check if PIN already exists for this card
                using var checkCmd = new MySqlCommand(
                    "SELECT COUNT(*) FROM PinDetails WHERE CardId = @CardId", conn, transaction);
                checkCmd.Parameters.AddWithValue("@CardId", cardId);
                var pinExists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;

                if (pinExists)
                {
                    // Update existing PIN
                    using var updateCmd = new MySqlCommand(
                        "UPDATE PinDetails SET Pin = @Pin WHERE CardId = @CardId", conn, transaction);
                    updateCmd.Parameters.AddWithValue("@CardId", cardId);
                    updateCmd.Parameters.AddWithValue("@Pin", pin);
                    updateCmd.ExecuteNonQuery();
                }
                else
                {
                    // Insert new PIN
                    using var insertCmd = new MySqlCommand(
                        "INSERT INTO PinDetails (CardId, Pin) VALUES (@CardId, @Pin)", conn, transaction);
                    insertCmd.Parameters.AddWithValue("@CardId", cardId);
                    insertCmd.Parameters.AddWithValue("@Pin", pin);
                    insertCmd.ExecuteNonQuery();

                    // Add initial transaction only for new PINs, showing 1000 initial balance
                    using var transCmd = new MySqlCommand(
                        "INSERT INTO Transactions (CardId, Amount, TransactionType, TransactionDate, Description) " +
                        "VALUES (@CardId, @Amount, 'Account Setup', @TransactionDate, @Description)", conn, transaction);
                    transCmd.Parameters.AddWithValue("@CardId", cardId);
                    transCmd.Parameters.AddWithValue("@Amount", 1000);
                    transCmd.Parameters.AddWithValue("@TransactionDate", DateTime.Now);
                    transCmd.Parameters.AddWithValue("@Description", "Account activated with initial balance of 1000");
                    transCmd.ExecuteNonQuery();
                }

                // Commit all changes
                transaction.Commit();
                return RedirectToAction("MainMenu", new { cardId });
            }
            catch (Exception ex)
            {
                // Roll back on error
                transaction.Rollback();
                _logger.LogError($"Pin save failed: {ex.Message}");

                TempData["AlertMessage"] = "Failed to save PIN. Please try again.";
                return RedirectToAction("PinPage", new { cardId });
            }
        }

        public IActionResult MainMenu(int cardId)
        {
            // For this method, we'll stick with the direct database access since
            // it's working well and doesn't need PIN verification
            using var conn = new MySqlConnection(connStr);
            conn.Open();

            using var cmd = new MySqlCommand("SELECT CardHolderName, CurrentBalance, SavingsBalance FROM carddetails WHERE Id = @CardId", conn);
            cmd.Parameters.AddWithValue("@CardId", cardId);
            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                ViewBag.CardHolderName = reader.GetString("CardHolderName");
                ViewBag.Balance = reader.GetDecimal("CurrentBalance");
                ViewBag.SavingsBalance = reader.GetDecimal("SavingsBalance");
            }

            ViewBag.CardId = cardId;
            return View();
        }

        [HttpPost]
        public IActionResult WithdrawCash(int cardId, decimal amount, string accountType)
        {
            if (amount < 1000 || amount % 100 != 0)
            {
                TempData["AlertMessage"] = "Withdrawal amount must be starting from 1000.";
                return RedirectToAction("MainMenu", new { cardId });
            }

            using var conn = new MySqlConnection(connStr);
            conn.Open();

            string balanceColumn = accountType == "savings" ? "SavingsBalance" : "CurrentBalance";
            string accountTypeDescription = accountType == "savings" ? "Savings" : "Current";

            using var balanceCmd = new MySqlCommand($"SELECT {balanceColumn}, CardHolderName FROM carddetails WHERE Id = @CardId", conn);
            balanceCmd.Parameters.AddWithValue("@CardId", cardId);
            using var balanceReader = balanceCmd.ExecuteReader();

            if (!balanceReader.Read())
            {
                TempData["AlertMessage"] = "Card not found.";
                return RedirectToAction("MainMenu", new { cardId });
            }

            var currentBalance = balanceReader.GetDecimal(balanceColumn);
            var cardHolderName = balanceReader.GetString("CardHolderName");
            balanceReader.Close();

            if (currentBalance == 0 || currentBalance < amount)
            {
                TempData["AlertMessage"] = "Insufficient balance.";
                return RedirectToAction("MainMenu", new { cardId });
            }

            using var cmd = new MySqlCommand($"UPDATE carddetails SET {balanceColumn} = {balanceColumn} - @Amount WHERE Id = @CardId", conn);
            cmd.Parameters.AddWithValue("@Amount", amount);
            cmd.Parameters.AddWithValue("@CardId", cardId);
            cmd.ExecuteNonQuery();

            // Generate a reference number for the transaction
            var referenceNumber = "W" + DateTime.Now.ToString("yyMMddHHmm") + cardId.ToString().PadLeft(3, '0');

            using var cmd2 = new MySqlCommand("INSERT INTO transactions (CardId, Amount, TransactionType, TransactionDate, Description) VALUES (@CardId, @Amount, 'Withdraw', @TransactionDate, @Description)", conn);
            cmd2.Parameters.AddWithValue("@CardId", cardId);
            cmd2.Parameters.AddWithValue("@Amount", amount);
            cmd2.Parameters.AddWithValue("@TransactionDate", DateTime.Now);
            cmd2.Parameters.AddWithValue("@Description", $"Cash withdrawal from {accountTypeDescription} account");
            cmd2.ExecuteNonQuery();

            TempData["SuccessMessage"] = $"Successfully withdrew {amount} from {accountTypeDescription} account. Reference: {referenceNumber}";
            return RedirectToAction("MainMenu", new { cardId });
        }

        [HttpPost]
        public IActionResult DepositCash(int cardId, decimal amount, string accountType)
        {
            if (amount < 1000 || amount % 100 != 0)
            {
                TempData["AlertMessage"] = "Deposit amount must be starting from 1000.";
                return RedirectToAction("MainMenu", new { cardId });
            }

            using var conn = new MySqlConnection(connStr);
            conn.Open();

            string balanceColumn = accountType == "savings" ? "SavingsBalance" : "CurrentBalance";
            string accountTypeDescription = accountType == "savings" ? "Savings" : "Current";

            using var balanceCmd = new MySqlCommand("SELECT CardHolderName FROM carddetails WHERE Id = @CardId", conn);
            balanceCmd.Parameters.AddWithValue("@CardId", cardId);
            var cardHolderName = (string)balanceCmd.ExecuteScalar();

            using var cmd = new MySqlCommand($"UPDATE carddetails SET {balanceColumn} = {balanceColumn} + @Amount WHERE Id = @CardId", conn);
            cmd.Parameters.AddWithValue("@Amount", amount);
            cmd.Parameters.AddWithValue("@CardId", cardId);
            cmd.ExecuteNonQuery();

            // Generate a reference number for the transaction
            var referenceNumber = "D" + DateTime.Now.ToString("yyMMddHHmm") + cardId.ToString().PadLeft(3, '0');

            using var cmd2 = new MySqlCommand("INSERT INTO transactions (CardId, Amount, TransactionType, TransactionDate, Description) VALUES (@CardId, @Amount, 'Deposit', @TransactionDate, @Description)", conn);
            cmd2.Parameters.AddWithValue("@CardId", cardId);
            cmd2.Parameters.AddWithValue("@Amount", amount);
            cmd2.Parameters.AddWithValue("@TransactionDate", DateTime.Now);
            cmd2.Parameters.AddWithValue("@Description", $"Cash deposit to {accountTypeDescription} account");
            cmd2.ExecuteNonQuery();

            TempData["SuccessMessage"] = $"Successfully deposited {amount} to {accountTypeDescription} account. Reference: {referenceNumber}";
            return RedirectToAction("MainMenu", new { cardId });
        }

        [HttpPost]
        public IActionResult TransferFunds(int cardId, string targetCardNumber, decimal amount, string accountType)
        {
            // Check if amount is at least 1000 and is a multiple of 1000
            if (amount < 1000 || amount % 1000 != 0)
            {
                TempData["AlertMessage"] = "Transfer amount must be starting from 1000.";
                return RedirectToAction("MainMenu", new { cardId });
            }

            // Check if targetCardNumber is null or empty
            if (string.IsNullOrEmpty(targetCardNumber))
            {
                TempData["AlertMessage"] = "Please enter a card number to transfer funds to.";
                return RedirectToAction("MainMenu", new { cardId });
            }

            // Store the target card number as is (with spaces)
            string formattedTargetCardNumber = targetCardNumber;

            using var conn = new MySqlConnection(connStr);
            conn.Open();

            // Check if targetCardNumber exists - search by the exact formatted number
            using var checkCmd = new MySqlCommand("SELECT Id, CardHolderName, CardNumber FROM carddetails WHERE CardNumber = @CardNumber", conn);
            checkCmd.Parameters.AddWithValue("@CardNumber", formattedTargetCardNumber);
            using var reader = checkCmd.ExecuteReader();

            if (!reader.Read())
            {
                // Handle the case where the target card does not exist
                TempData["AlertMessage"] = "Target card does not exist.";
                return RedirectToAction("MainMenu", new { cardId });
            }

            var targetCardId = reader.GetInt32("Id");
            var targetCardHolderName = reader.GetString("CardHolderName");
            var targetCardNumberFromDb = reader.GetString("CardNumber");
            reader.Close();

            // Get sender's balance and information
            string balanceColumn = accountType == "savings" ? "SavingsBalance" : "CurrentBalance";

            using var balanceCmd = new MySqlCommand($"SELECT {balanceColumn}, CardHolderName, CardNumber FROM carddetails WHERE Id = @CardId", conn);
            balanceCmd.Parameters.AddWithValue("@CardId", cardId);
            using var balanceReader = balanceCmd.ExecuteReader();

            if (!balanceReader.Read())
            {
                TempData["AlertMessage"] = "Card not found.";
                return RedirectToAction("MainMenu", new { cardId });
            }

            var currentBalance = balanceReader.GetDecimal(balanceColumn);
            var senderCardHolderName = balanceReader.GetString("CardHolderName");
            var senderCardNumber = balanceReader.GetString("CardNumber");
            balanceReader.Close();

            if (currentBalance == 0 || currentBalance < amount)
            {
                TempData["AlertMessage"] = "Insufficient balance.";
                return RedirectToAction("MainMenu", new { cardId });
            }

            // Deduct from sender's account
            using var cmd = new MySqlCommand($"UPDATE carddetails SET {balanceColumn} = {balanceColumn} - @Amount WHERE Id = @CardId", conn);
            cmd.Parameters.AddWithValue("@Amount", amount);
            cmd.Parameters.AddWithValue("@CardId", cardId);
            cmd.ExecuteNonQuery();

            // Add to recipient's account
            using var cmd2 = new MySqlCommand("UPDATE carddetails SET CurrentBalance = CurrentBalance + @Amount WHERE Id = @TargetCardId", conn);
            cmd2.Parameters.AddWithValue("@Amount", amount);
            cmd2.Parameters.AddWithValue("@TargetCardId", targetCardId);
            cmd2.ExecuteNonQuery();

            // For display purposes - mask card numbers
            var maskedSenderName = senderCardHolderName.Substring(0, 1) + "***";
            var maskedSenderCardNumber = senderCardNumber.Substring(0, 4) + " **** " + senderCardNumber.Substring(15, 4);
            var maskedTargetName = targetCardHolderName.Substring(0, 1) + "***";
            var maskedTargetCardNumber = targetCardNumberFromDb.Substring(0, 4) + " **** " + targetCardNumberFromDb.Substring(15, 4);

            // Record transaction for sender
            using var cmd3 = new MySqlCommand("INSERT INTO transactions (CardId, Amount, TransactionType, TransactionDate, Description) VALUES (@CardId, @Amount, 'Transfer Out', @TransactionDate, @Description)", conn);
            cmd3.Parameters.AddWithValue("@CardId", cardId);
            cmd3.Parameters.AddWithValue("@Amount", amount);
            cmd3.Parameters.AddWithValue("@TransactionDate", DateTime.Now);
            cmd3.Parameters.AddWithValue("@Description", $"Transferred to {maskedTargetName} ({maskedTargetCardNumber})");
            cmd3.ExecuteNonQuery();

            // Record transaction for recipient
            using var cmd4 = new MySqlCommand("INSERT INTO transactions (CardId, Amount, TransactionType, TransactionDate, Description) VALUES (@TargetCardId, @Amount, 'Transfer In', @TransactionDate, @Description)", conn);
            cmd4.Parameters.AddWithValue("@TargetCardId", targetCardId);
            cmd4.Parameters.AddWithValue("@Amount", amount);
            cmd4.Parameters.AddWithValue("@TransactionDate", DateTime.Now);
            cmd4.Parameters.AddWithValue("@Description", $"Received from {maskedSenderName} ({maskedSenderCardNumber})");
            cmd4.ExecuteNonQuery();

            // Generate a reference number for the transaction
            var referenceNumber = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
            TempData["SuccessMessage"] = $"Transaction successful! Your reference number is {referenceNumber}.";

            return RedirectToAction("MainMenu", new { cardId });
        }

        [HttpPost]
        public async Task<IActionResult> ChangePin(int cardId, string currentPin, string newPin)
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(newPin, @"^\d{4}$"))
            {
                TempData["AlertMessage"] = "New PIN must be exactly 4 digits.";
                return RedirectToAction("MainMenu", new { cardId });
            }

            try
            {
                // Try to change PIN using the API
                var payload = new { CardId = cardId, CurrentPin = currentPin, NewPin = newPin };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"{apiBaseUrl}?endpoint=pin", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "PIN changed successfully.";
                    return RedirectToAction("MainMenu", new { cardId });
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    var errorData = JsonSerializer.Deserialize<Dictionary<string, string>>(errorResponse);

                    if (errorData != null && errorData.TryGetValue("error", out var errorMessage))
                    {
                        TempData["AlertMessage"] = errorMessage;
                        return RedirectToAction("MainMenu", new { cardId });
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception and fall back to direct database access
                _logger.LogError($"API call failed: {ex.Message}");
            }

            // Fall back to direct database access
            using var conn = new MySqlConnection(connStr);
            conn.Open();

            // Verify current PIN
            using var verifyCmd = new MySqlCommand("SELECT Pin FROM pindetails WHERE CardId = @CardId", conn);
            verifyCmd.Parameters.AddWithValue("@CardId", cardId);
            var existingPin = (string)verifyCmd.ExecuteScalar();

            if (existingPin != currentPin)
            {
                TempData["AlertMessage"] = "Current PIN is incorrect.";
                return RedirectToAction("MainMenu", new { cardId });
            }

            // Update to new PIN
            using var cmd = new MySqlCommand("UPDATE pindetails SET Pin = @NewPin WHERE CardId = @CardId", conn);
            cmd.Parameters.AddWithValue("@CardId", cardId);
            cmd.Parameters.AddWithValue("@NewPin", newPin);
            cmd.ExecuteNonQuery();

            TempData["SuccessMessage"] = "PIN changed successfully.";
            return RedirectToAction("MainMenu", new { cardId });
        }

        public IActionResult CheckBalance(int cardId)
        {
            // Keep existing implementation
            using var conn = new MySqlConnection(connStr);
            conn.Open();

            using var cmd = new MySqlCommand("SELECT CurrentBalance, SavingsBalance FROM carddetails WHERE Id = @CardId", conn);
            cmd.Parameters.AddWithValue("@CardId", cardId);
            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                ViewBag.Balance = reader.GetDecimal("CurrentBalance");
                ViewBag.SavingsBalance = reader.GetDecimal("SavingsBalance");
            }

            ViewBag.CardId = cardId;
            return View();
        }

        public IActionResult MiniStatement(int cardId)
        {
            // Keep existing implementation
            using var conn = new MySqlConnection(connStr);
            conn.Open();

            using var cmd = new MySqlCommand("SELECT Id, Amount, TransactionType, TransactionDate, Description FROM transactions WHERE CardId = @CardId ORDER BY TransactionDate DESC", conn);
            cmd.Parameters.AddWithValue("@CardId", cardId);
            using var reader = cmd.ExecuteReader();

            var transactions = new List<Transaction>();
            while (reader.Read())
            {
                transactions.Add(new Transaction
                {
                    Id = reader.GetInt32("Id"),
                    Amount = reader.GetDecimal("Amount"),
                    TransactionType = reader.GetString("TransactionType"),
                    TransactionDate = reader.GetDateTime("TransactionDate"),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString("Description")
                });
            }

            ViewBag.Transactions = transactions;
            ViewBag.CardId = cardId;
            return View();
        }

        [HttpPost]
        public IActionResult DeleteTransaction(int transactionId, int cardId)
        {
            // Keep existing implementation
            using var conn = new MySqlConnection(connStr);
            conn.Open();

            using var cmd = new MySqlCommand("DELETE FROM transactions WHERE Id = @TransactionId", conn);
            cmd.Parameters.AddWithValue("@TransactionId", transactionId);
            cmd.ExecuteNonQuery();

            return RedirectToAction("MiniStatement", new { cardId });
        }

        [HttpPost]
        public IActionResult Logout()
        {
            // Keep existing implementation
            return RedirectToAction("GetStarted");
        }

        [HttpPost]
        public async Task<IActionResult> VerifyPinAndExecute(int cardId, string pin, string actionType, decimal amount, string accountType, string targetCardNumber)
        {
            // Check if PIN is null or empty
            if (string.IsNullOrEmpty(pin))
            {
                TempData["AlertMessage"] = "Please enter your PIN.";
                return RedirectToAction("MainMenu", new { cardId });
            }

            try
            {
                // Try to verify PIN using the API
                var payload = new { CardId = cardId, Pin = pin };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{apiBaseUrl}?endpoint=pin&verify=true", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<Dictionary<string, bool>>(responseContent);

                    if (responseData != null && responseData.TryGetValue("valid", out var isValid) && isValid)
                    {
                        // PIN is valid, execute the requested action
                        switch (actionType)
                        {
                            case "WithdrawCash":
                                return WithdrawCash(cardId, amount, accountType);
                            case "DepositCash":
                                return DepositCash(cardId, amount, accountType);
                            case "TransferFunds":
                                // Process transfer with the targetCardNumber as is (with spaces)
                                return TransferFunds(cardId, targetCardNumber, amount, accountType);
                            default:
                                TempData["AlertMessage"] = "Invalid action.";
                                return RedirectToAction("MainMenu", new { cardId });
                        }
                    }
                    else
                    {
                        TempData["AlertMessage"] = "Incorrect PIN.";
                        return RedirectToAction("MainMenu", new { cardId });
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception and fall back to direct database access
                _logger.LogError($"API call failed: {ex.Message}");
            }

            // Fall back to direct database access
            using var conn = new MySqlConnection(connStr);
            conn.Open();

            // Verify PIN
            using var verifyCmd = new MySqlCommand("SELECT Pin FROM pindetails WHERE CardId = @CardId", conn);
            verifyCmd.Parameters.AddWithValue("@CardId", cardId);
            var existingPin = (string)verifyCmd.ExecuteScalar();

            if (existingPin != pin)
            {
                TempData["AlertMessage"] = "Incorrect PIN.";
                return RedirectToAction("MainMenu", new { cardId });
            }

            // Execute the requested action
            switch (actionType)
            {
                case "WithdrawCash":
                    return WithdrawCash(cardId, amount, accountType);
                case "DepositCash":
                    return DepositCash(cardId, amount, accountType);
                case "TransferFunds":
                    // Process transfer with the targetCardNumber as is (with spaces)
                    return TransferFunds(cardId, targetCardNumber, amount, accountType);
                default:
                    TempData["AlertMessage"] = "Invalid action.";
                    return RedirectToAction("MainMenu", new { cardId });
            }
        }

        // New method to test API endpoints
        public async Task<IActionResult> TestApi(int? cardId = null)
        {
            var model = new Dictionary<string, object>();
            model["ApiBaseUrl"] = apiBaseUrl;

            try
            {
                // Test PIN endpoint - GET
                if (cardId.HasValue)
                {
                    var pinGetResponse = await _httpClient.GetAsync($"{apiBaseUrl}?endpoint=pin&cardId={cardId}");
                    model["PinGetStatus"] = $"Status: {pinGetResponse.StatusCode}";
                    model["PinGetResponse"] = await pinGetResponse.Content.ReadAsStringAsync();
                }

                // Test transactions endpoint - GET
                if (cardId.HasValue)
                {
                    var transResponse = await _httpClient.GetAsync($"{apiBaseUrl}?endpoint=transactions&cardId={cardId}");
                    model["TransactionsStatus"] = $"Status: {transResponse.StatusCode}";
                    model["TransactionsResponse"] = await transResponse.Content.ReadAsStringAsync();
                }
                else
                {
                    var transAllResponse = await _httpClient.GetAsync($"{apiBaseUrl}?endpoint=transactions");
                    model["AllTransactionsStatus"] = $"Status: {transAllResponse.StatusCode}";
                    model["AllTransactionsResponse"] = await transAllResponse.Content.ReadAsStringAsync();
                }

                // Get a list of all cards
                using var conn = new MySqlConnection(connStr);
                conn.Open();
                using var cmd = new MySqlCommand("SELECT Id, CardHolderName, CardNumber FROM carddetails", conn);
                using var reader = cmd.ExecuteReader();

                var cards = new List<CardDetails>();
                while (reader.Read())
                {
                    cards.Add(new CardDetails
                    {
                        Id = reader.GetInt32("Id"),
                        CardHolderName = reader.GetString("CardHolderName"),
                        CardNumber = reader.GetString("CardNumber") // Already formatted in database
                    });
                }
                model["Cards"] = cards;
            }
            catch (Exception ex)
            {
                model["Error"] = $"API test failed: {ex.Message}";
            }

            return View(model);
        }

        // Method to update existing cards to have initial balance of 1000
        public IActionResult UpdateExistingAccountBalances()
        {
            using var conn = new MySqlConnection(connStr);
            conn.Open();

            // Start a transaction
            using var transaction = conn.BeginTransaction();

            try
            {
                // Update all cards with 0 balance to have 1000
                using var updateCmd = new MySqlCommand(
                    "UPDATE carddetails SET CurrentBalance = 1000 WHERE CurrentBalance = 0", conn, transaction);
                var rowsAffected = updateCmd.ExecuteNonQuery();

                // Add transaction records for any updated accounts
                if (rowsAffected > 0)
                {
                    // Get the IDs of cards that were updated
                    using var getCardsCmd = new MySqlCommand(
                        "SELECT Id FROM carddetails WHERE CurrentBalance = 1000", conn, transaction);
                    using var reader = getCardsCmd.ExecuteReader();

                    var updatedCardIds = new List<int>();
                    while (reader.Read())
                    {
                        updatedCardIds.Add(reader.GetInt32(0));
                    }
                    reader.Close();

                    // Add transaction records for each updated card
                    foreach (var updatedCardId in updatedCardIds)
                    {
                        using var transCmd = new MySqlCommand(
                            "INSERT INTO transactions (CardId, Amount, TransactionType, TransactionDate, Description) " +
                            "VALUES (@CardId, @Amount, 'Account Update', @TransactionDate, @Description)", conn, transaction);
                        transCmd.Parameters.AddWithValue("@CardId", updatedCardId);
                        transCmd.Parameters.AddWithValue("@Amount", 1000);
                        transCmd.Parameters.AddWithValue("@TransactionDate", DateTime.Now);
                        transCmd.Parameters.AddWithValue("@Description", "Initial balance updated to 1000");
                        transCmd.ExecuteNonQuery();
                    }
                }

                transaction.Commit();
                TempData["SuccessMessage"] = $"Updated {rowsAffected} accounts to have initial balance of 1000.";
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError($"Failed to update account balances: {ex.Message}");
                TempData["AlertMessage"] = "Failed to update account balances.";
            }

            return RedirectToAction("Index");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}