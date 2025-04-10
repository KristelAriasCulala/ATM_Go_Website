using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using practice_2.Models;

namespace practice_2.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly String connStr = ("Server=localhost;Database=atmd_db;User=root;Password=");

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
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
        public IActionResult SaveCardDetails(CardDetails cardDetails)
        {
            if (!ModelState.IsValid)
            {
                return View("CardDetails");
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(cardDetails.CardNumber, @"^\d{4}-\d{4}-\d{4}-\d{4}$"))
            {
                ModelState.AddModelError("CardNumber", "Card number must be in the format 0000-0000-0000-0000.");
                return View("CardDetails");
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(cardDetails.CVV, @"^\d{3}$"))
            {
                ModelState.AddModelError("CVV", "CVV must be exactly 3 digits.");
                return View("CardDetails");
            }

            using var conn = new MySqlConnection(connStr);
            conn.Open();

            // Check if card details already exist
            using var checkCmd = new MySqlCommand("SELECT Id FROM CardDetails WHERE CardNumber = @CardNumber AND ExpiryDate = @ExpiryDate", conn);
            checkCmd.Parameters.AddWithValue("@CardNumber", cardDetails.CardNumber);
            checkCmd.Parameters.AddWithValue("@ExpiryDate", cardDetails.ExpiryDate);
            var existingCardId = checkCmd.ExecuteScalar();

            if (existingCardId != null)
            {
                // Card details already exist, use the existing card ID
                return RedirectToAction("PinPage", new { cardId = (int)existingCardId });
            }

            // Insert new card details including Email field
            using var cmd = new MySqlCommand("INSERT INTO CardDetails (CardHolderName, Email, CardNumber, ExpiryDate, CVV, CurrentBalance, SavingsBalance) VALUES (@CardHolderName, @Email, @CardNumber, @ExpiryDate, @CVV, 1000, 1000)", conn);
            cmd.Parameters.AddWithValue("@CardHolderName", cardDetails.CardHolderName);
            cmd.Parameters.AddWithValue("@Email", cardDetails.Email);
            cmd.Parameters.AddWithValue("@CardNumber", cardDetails.CardNumber);
            cmd.Parameters.AddWithValue("@ExpiryDate", cardDetails.ExpiryDate);
            cmd.Parameters.AddWithValue("@CVV", cardDetails.CVV);
            cmd.ExecuteNonQuery();

            return RedirectToAction("PinPage", new { cardId = cmd.LastInsertedId });
        }

        public IActionResult PinPage(int cardId)
        {
            ViewBag.CardId = cardId;
            return View();
        }

        [HttpPost]
        public IActionResult SavePin(int cardId, string pin)
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(pin, @"^\d{4}$"))
            {
                TempData["AlertMessage"] = "PIN must be exactly 4 digits.";
                return RedirectToAction("PinPage", new { cardId });
            }

            using var conn = new MySqlConnection(connStr);
            conn.Open();

            using var cmd = new MySqlCommand("INSERT INTO PinDetails (CardId, Pin) VALUES (@CardId, @Pin) ON DUPLICATE KEY UPDATE Pin = @Pin", conn);
            cmd.Parameters.AddWithValue("@CardId", cardId);
            cmd.Parameters.AddWithValue("@Pin", pin);
            cmd.ExecuteNonQuery();

            return RedirectToAction("MainMenu", new { cardId });
        }

        public IActionResult MainMenu(int cardId)
        {
            using var conn = new MySqlConnection(connStr);
            conn.Open();

            using var cmd = new MySqlCommand("SELECT CardHolderName, CurrentBalance, SavingsBalance FROM CardDetails WHERE Id = @CardId", conn);
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
            if (amount < 500 || amount % 100 != 0)
            {
                TempData["AlertMessage"] = "Withdrawal amount must be in increments of 100 starting from 500.";
                return RedirectToAction("MainMenu", new { cardId });
            }

            using var conn = new MySqlConnection(connStr);
            conn.Open();

            string balanceColumn = accountType == "savings" ? "SavingsBalance" : "CurrentBalance";

            using var balanceCmd = new MySqlCommand($"SELECT {balanceColumn} FROM CardDetails WHERE Id = @CardId", conn);
            balanceCmd.Parameters.AddWithValue("@CardId", cardId);
            var currentBalance = (decimal)balanceCmd.ExecuteScalar();

            if (currentBalance == 0 || currentBalance < amount)
            {
                TempData["AlertMessage"] = "Insufficient balance.";
                return RedirectToAction("MainMenu", new { cardId });
            }

            using var cmd = new MySqlCommand($"UPDATE CardDetails SET {balanceColumn} = {balanceColumn} - @Amount WHERE Id = @CardId", conn);
            cmd.Parameters.AddWithValue("@Amount", amount);
            cmd.Parameters.AddWithValue("@CardId", cardId);
            cmd.ExecuteNonQuery();

            using var cmd2 = new MySqlCommand("INSERT INTO Transactions (CardId, Amount, TransactionType, TransactionDate) VALUES (@CardId, @Amount, 'Withdraw', @TransactionDate)", conn);
            cmd2.Parameters.AddWithValue("@CardId", cardId);
            cmd2.Parameters.AddWithValue("@Amount", amount);
            cmd2.Parameters.AddWithValue("@TransactionDate", DateTime.Now);
            cmd2.ExecuteNonQuery();

            return RedirectToAction("MainMenu", new { cardId });
        }

        [HttpPost]
        public IActionResult DepositCash(int cardId, decimal amount, string accountType)
        {
            if (amount < 500 || amount % 100 != 0)
            {
                TempData["AlertMessage"] = "Deposit amount must be in increments of 100 starting from 500.";
                return RedirectToAction("MainMenu", new { cardId });
            }

            using var conn = new MySqlConnection(connStr);
            conn.Open();

            string balanceColumn = accountType == "savings" ? "SavingsBalance" : "CurrentBalance";

            using var cmd = new MySqlCommand($"UPDATE CardDetails SET {balanceColumn} = {balanceColumn} + @Amount WHERE Id = @CardId", conn);
            cmd.Parameters.AddWithValue("@Amount", amount);
            cmd.Parameters.AddWithValue("@CardId", cardId);
            cmd.ExecuteNonQuery();

            using var cmd2 = new MySqlCommand("INSERT INTO Transactions (CardId, Amount, TransactionType, TransactionDate) VALUES (@CardId, @Amount, 'Deposit', @TransactionDate)", conn);
            cmd2.Parameters.AddWithValue("@CardId", cardId);
            cmd2.Parameters.AddWithValue("@Amount", amount);
            cmd2.Parameters.AddWithValue("@TransactionDate", DateTime.Now);
            cmd2.ExecuteNonQuery();

            return RedirectToAction("MainMenu", new { cardId });
        }

        [HttpPost]
        public IActionResult TransferFunds(int cardId, string targetCardNumber, decimal amount, string accountType)
        {
            if (amount < 500 || amount % 100 != 0)
            {
                TempData["AlertMessage"] = "Transfer amount must be in increments of 100 starting from 500.";
                return RedirectToAction("MainMenu", new { cardId });
            }

            using var conn = new MySqlConnection(connStr);
            conn.Open();

            // Check if targetCardNumber exists
            using var checkCmd = new MySqlCommand("SELECT Id, CardHolderName, CardNumber FROM CardDetails WHERE CardNumber = @TargetCardNumber", conn);
            checkCmd.Parameters.AddWithValue("@TargetCardNumber", targetCardNumber);
            using var reader = checkCmd.ExecuteReader();

            if (!reader.Read())
            {
                // Handle the case where the target card does not exist
                TempData["AlertMessage"] = "Target card does not exist.";
                return RedirectToAction("MainMenu", new { cardId });
            }

            var targetCardId = reader.GetInt32("Id");
            var targetCardHolderName = reader.GetString("CardHolderName");
            var targetCardNumberFull = reader.GetString("CardNumber");
            reader.Close();

            string balanceColumn = accountType == "savings" ? "SavingsBalance" : "CurrentBalance";

            using var balanceCmd = new MySqlCommand($"SELECT {balanceColumn}, CardHolderName, CardNumber FROM CardDetails WHERE Id = @CardId", conn);
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

            using var cmd = new MySqlCommand($"UPDATE CardDetails SET {balanceColumn} = {balanceColumn} - @Amount WHERE Id = @CardId", conn);
            cmd.Parameters.AddWithValue("@Amount", amount);
            cmd.Parameters.AddWithValue("@CardId", cardId);
            cmd.ExecuteNonQuery();

            using var cmd2 = new MySqlCommand("UPDATE CardDetails SET CurrentBalance = CurrentBalance + @Amount WHERE Id = @TargetCardId", conn);
            cmd2.Parameters.AddWithValue("@Amount", amount);
            cmd2.Parameters.AddWithValue("@TargetCardId", targetCardId);
            cmd2.ExecuteNonQuery();

            var maskedSenderName = senderCardHolderName.Substring(0, 1) + "***";
            var maskedSenderCardNumber = senderCardNumber.Substring(0, 4) + "****" + senderCardNumber.Substring(8, 4) + "****";
            var maskedTargetName = targetCardHolderName.Substring(0, 1) + "***";
            var maskedTargetCardNumber = targetCardNumberFull.Substring(0, 4) + "****" + targetCardNumberFull.Substring(8, 4) + "****";

            using var cmd3 = new MySqlCommand("INSERT INTO Transactions (CardId, Amount, TransactionType, TransactionDate, Description) VALUES (@CardId, @Amount, 'Transfer Out', @TransactionDate, @Description)", conn);
            cmd3.Parameters.AddWithValue("@CardId", cardId);
            cmd3.Parameters.AddWithValue("@Amount", amount);
            cmd3.Parameters.AddWithValue("@TransactionDate", DateTime.Now);
            cmd3.Parameters.AddWithValue("@Description", $"Transferred to {maskedTargetName} ({maskedTargetCardNumber})");
            cmd3.ExecuteNonQuery();

            using var cmd4 = new MySqlCommand("INSERT INTO Transactions (CardId, Amount, TransactionType, TransactionDate, Description) VALUES (@TargetCardId, @Amount, 'Transfer In', @TransactionDate, @Description)", conn);
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
        public IActionResult ChangePin(int cardId, string currentPin, string newPin)
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(newPin, @"^\d{4}$"))
            {
                TempData["AlertMessage"] = "New PIN must be exactly 4 digits.";
                return RedirectToAction("MainMenu", new { cardId });
            }

            using var conn = new MySqlConnection(connStr);
            conn.Open();

            // Verify current PIN
            using var verifyCmd = new MySqlCommand("SELECT Pin FROM PinDetails WHERE CardId = @CardId", conn);
            verifyCmd.Parameters.AddWithValue("@CardId", cardId);
            var existingPin = (string)verifyCmd.ExecuteScalar();

            if (existingPin != currentPin)
            {
                TempData["AlertMessage"] = "Current PIN is incorrect.";
                return RedirectToAction("MainMenu", new { cardId });
            }

            // Update to new PIN
            using var cmd = new MySqlCommand("UPDATE PinDetails SET Pin = @NewPin WHERE CardId = @CardId", conn);
            cmd.Parameters.AddWithValue("@CardId", cardId);
            cmd.Parameters.AddWithValue("@NewPin", newPin);
            cmd.ExecuteNonQuery();

            TempData["SuccessMessage"] = "PIN changed successfully.";
            return RedirectToAction("MainMenu", new { cardId });
        }

        public IActionResult CheckBalance(int cardId)
        {
            using var conn = new MySqlConnection(connStr);
            conn.Open();

            using var cmd = new MySqlCommand("SELECT CurrentBalance, SavingsBalance FROM CardDetails WHERE Id = @CardId", conn);
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
            using var conn = new MySqlConnection(connStr);
            conn.Open();

            using var cmd = new MySqlCommand("SELECT Id, Amount, TransactionType, TransactionDate, Description FROM Transactions WHERE CardId = @CardId ORDER BY TransactionDate DESC", conn);
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
            using var conn = new MySqlConnection(connStr);
            conn.Open();

            using var cmd = new MySqlCommand("DELETE FROM Transactions WHERE Id = @TransactionId", conn);
            cmd.Parameters.AddWithValue("@TransactionId", transactionId);
            cmd.ExecuteNonQuery();

            return RedirectToAction("MiniStatement", new { cardId });
        }

        [HttpPost]
        public IActionResult Logout()
        {
            // Perform any necessary cleanup or session termination here
            return RedirectToAction("GetStarted");
        }

        [HttpPost]
        public IActionResult VerifyPinAndExecute(int cardId, string pin, string actionType, decimal amount, string accountType, string targetCardNumber)
        {
            using var conn = new MySqlConnection(connStr);
            conn.Open();

            // Verify PIN
            using var verifyCmd = new MySqlCommand("SELECT Pin FROM PinDetails WHERE CardId = @CardId", conn);
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
                    return TransferFunds(cardId, targetCardNumber, amount, accountType);
                default:
                    TempData["AlertMessage"] = "Invalid action.";
                    return RedirectToAction("MainMenu", new { cardId });
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}