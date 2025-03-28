-- phpMyAdmin SQL Dump
-- version 5.2.1
-- https://www.phpmyadmin.net/
--
-- Host: 127.0.0.1
-- Generation Time: Mar 28, 2025 at 02:07 PM
-- Server version: 10.4.32-MariaDB
-- PHP Version: 8.2.12

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- Database: `atmd`
--

-- --------------------------------------------------------

--
-- Table structure for table `carddetails`
--

CREATE TABLE `carddetails` (
  `Id` int(11) NOT NULL,
  `CardHolderName` varchar(100) NOT NULL,
  `CardNumber` char(19) NOT NULL,
  `ExpiryDate` char(5) NOT NULL,
  `CurrentBalance` decimal(10,2) DEFAULT 1000.00,
  `CVV` varchar(3) DEFAULT NULL,
  `SavingsBalance` decimal(18,2) DEFAULT 0.00
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `carddetails`
--

INSERT INTO `carddetails` (`Id`, `CardHolderName`, `CardNumber`, `ExpiryDate`, `CurrentBalance`, `CVV`, `SavingsBalance`) VALUES
(1, 'Kristel A. Culala', '1111-1111-1111-1111', '11/11', 88055.00, NULL, 0.00),
(2, 'jake m. batumbakal', '9999-9999-9999-9999', '99/99', 22545.00, NULL, 0.00),
(3, 'kim', '2222-2222-2222-2222', '22/22', 900.00, NULL, 0.00),
(4, 'lee', '2222-2222-2222-2222', '22/22', 1000.00, NULL, 0.00),
(5, 'lee', '9999-9999-9999-9999', '99/99', 700.00, NULL, 0.00),
(6, 'ponyo', '5555-5555-5555-5555', '55/55', 1000.00, NULL, 0.00),
(7, 'megan', '1111-1111-1111-1111', '11/11', 490.00, NULL, 0.00),
(8, 'megan', '1111-1111-1111-1111', '11/11', 1000.00, NULL, 0.00),
(9, 'megan', '1111-1111-1111-1111', '11/11', 900.00, NULL, 0.00),
(10, 'jake m. batumbakal', '9999-9999-9999', '99/99', 1000.00, NULL, 0.00),
(11, 'Kristel Culala', '1234-5678-9876-5432', '05/25', 1970659.00, '123', 997176.00),
(12, 'Loveleen Kate', '9973-5432-8752-2222', '02/27', 10017529.00, '098', 1099500.00),
(13, 'Kristel Culala', '1234-5678-9876-5432', '02/25', 1000.00, '123', 0.00),
(14, 'kimmy', '3434-3433-4354-5346', '45/64', 1000.00, '345', 1000.00),
(15, 'ponyo', '2121-2121-1111-1111', '23/43', 1000.00, '423', 1000.00),
(16, 'poo', '6245-6784-5677-6543', '54/53', 1000.00, '876', 1000.00),
(17, 'jaeyun', '6754-3546-5798-7656', '42/34', 1000.00, '534', 1000.00),
(18, 'Jay', '4354-3543-5435-3523', '52/35', 1000.00, '532', 1000.00),
(19, 'gl', '2131-2323-1231-3321', '26/24', 1000.00, '646', 1000.00),
(20, 'ji', '3453-4534-5345-4345', '54/35', 900.00, '545', 1000.00),
(21, 'jk', '3453-4534-5343-5345', '54/35', 1000.00, '545', 1000.00),
(22, 'sun', '3455-5345-4543-5454', '54/35', 1000.00, '545', 1000.00),
(23, 'hk', '4324-4342-3443-2432', '23/43', 900.00, '434', 900.00),
(24, 'sh', '3435-4545-3454-5453', '54/54', 1000.00, '543', 1000.00),
(25, 'ty', '6363-6364-6363-6346', '56/34', 1000.00, '543', 1000.00),
(26, 'ji', '4534-5354-5453-4543', '35/45', 900.00, '453', 1000.00),
(27, 'Kristel Culala', '1234-5678-9876-5432', '05/52', 1000.00, '123', 1000.00),
(28, 'lk', '5466-4656-5646-4656', '64/56', 99999999.99, '645', 1000001000.00),
(29, 'dfhdh', '6364-6363-6364-3643', '36/36', 1000.00, '346', 1000.00),
(30, 'wetwet', '4543-5444-4444-4444', '44/44', 1000.00, '444', 1000.00),
(31, 'gdfgdf', '4333-3333-3333-3333', '33/33', 1000.00, '333', 1000.00),
(32, 'Kristel Culala', '5353-4545-4353-4543', '53/45', 1000.00, '543', 1000.00),
(33, 'dfsfsdfdsfdfd', '3445-4553-4545-4354', '45/45', 1000.00, '454', 1000.00),
(34, 'thdf', '5654-4444-4444-4444', '44/44', 1000.00, '444', 1000.00),
(35, 'rerwer', '3244-4444-4444-4444', '44/44', 1000.00, '444', 1000.00),
(36, 'Loveleen Kate', '9935-4328-7522-2222', '02/27', 1000.00, '098', 1000.00);

-- --------------------------------------------------------

--
-- Table structure for table `pindetails`
--

CREATE TABLE `pindetails` (
  `Id` int(11) NOT NULL,
  `CardId` int(11) NOT NULL,
  `Pin` char(4) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `pindetails`
--

INSERT INTO `pindetails` (`Id`, `CardId`, `Pin`) VALUES
(1, 1, '8888'),
(2, 2, '1000'),
(3, 3, '2222'),
(4, 4, '2222'),
(5, 5, '9999'),
(6, 6, '5555'),
(7, 7, '6767'),
(8, 8, '6767'),
(9, 9, '9999'),
(10, 1, '8888'),
(11, 2, '1000'),
(12, 2, '1000'),
(13, 2, '1000'),
(14, 2, '1000'),
(15, 2, '1000'),
(16, 1, '1111'),
(17, 10, '5678'),
(18, 10, '5678'),
(19, 2, '1000'),
(20, 2, '1000'),
(21, 11, '7879'),
(22, 11, '7879'),
(23, 11, '7879'),
(24, 12, '2354'),
(25, 13, '7318'),
(26, 13, '7319'),
(27, 11, '7879'),
(28, 11, '7879'),
(29, 12, '2354'),
(30, 12, '2354'),
(31, 12, '2354'),
(32, 11, '7879'),
(33, 12, '2354'),
(34, 11, '7879'),
(35, 11, '7879'),
(36, 12, '2354'),
(37, 11, '7879'),
(38, 11, '7879'),
(39, 11, '7879'),
(40, 11, '7879'),
(41, 11, '7879'),
(42, 11, '7879'),
(43, 11, '7879'),
(44, 11, '7879'),
(45, 11, '7879'),
(46, 11, '7879'),
(47, 11, '7879'),
(48, 11, '7879'),
(49, 11, '7879'),
(50, 11, '7879'),
(51, 11, '7879'),
(52, 11, '7879'),
(53, 11, '7879'),
(54, 11, '7879'),
(55, 11, '7879'),
(56, 11, '7879'),
(57, 12, '2354'),
(58, 11, '7879'),
(59, 11, '7879'),
(60, 11, '7879'),
(61, 11, '7879'),
(62, 11, '7879'),
(63, 11, '7879'),
(64, 11, '7879'),
(65, 3, '6775'),
(66, 14, '9987'),
(67, 11, '7879'),
(68, 11, '7879'),
(69, 18, '5322'),
(70, 18, '5555'),
(71, 18, '2365'),
(72, 18, '1664'),
(73, 19, '5353'),
(74, 20, '5454'),
(75, 21, '5454'),
(76, 22, '5454'),
(77, 23, '3432'),
(78, 24, '5353'),
(79, 24, '9190'),
(80, 25, '8320'),
(81, 11, '7879'),
(82, 26, '5345'),
(83, 11, '7879'),
(84, 11, '7879'),
(85, 11, '7879'),
(86, 11, '7879'),
(87, 27, '7858'),
(88, 11, '7879'),
(89, 11, '7879'),
(90, 11, '7879'),
(91, 11, '7879'),
(92, 12, '2354'),
(93, 12, '2354'),
(94, 12, '2354'),
(95, 11, '7879'),
(96, 11, '7879'),
(97, 28, '6465'),
(98, 29, '4634'),
(99, 31, '4555'),
(100, 31, '3454'),
(101, 32, '3454'),
(102, 11, '7879'),
(103, 11, '7879'),
(104, 34, '5654'),
(105, 11, '7879'),
(106, 11, '7879'),
(107, 11, '7879'),
(108, 11, '7879'),
(109, 11, '7879'),
(110, 11, '7879'),
(111, 11, '7879'),
(112, 11, '7879'),
(113, 11, '7879'),
(114, 11, '7879'),
(115, 36, '2354'),
(116, 12, '2354'),
(117, 11, '7879'),
(118, 12, '2354'),
(119, 11, '5123'),
(120, 11, '7879'),
(121, 11, '7989');

-- --------------------------------------------------------

--
-- Table structure for table `transactions`
--

CREATE TABLE `transactions` (
  `Id` int(11) NOT NULL,
  `CardId` int(11) NOT NULL,
  `Amount` decimal(10,2) NOT NULL,
  `TransactionType` varchar(50) NOT NULL,
  `TransactionDate` datetime NOT NULL,
  `Description` varchar(255) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `transactions`
--

INSERT INTO `transactions` (`Id`, `CardId`, `Amount`, `TransactionType`, `TransactionDate`, `Description`) VALUES
(1, 3, 0.00, 'Withdraw', '2025-03-13 22:04:09', NULL),
(2, 3, 0.00, 'Withdraw', '2025-03-13 22:04:10', NULL),
(3, 3, 100.00, 'Withdraw', '2025-03-13 22:04:17', NULL),
(4, 4, 0.00, 'Transfer Out', '2025-03-13 22:09:16', NULL),
(6, 5, 0.00, 'Withdraw', '2025-03-13 22:10:24', NULL),
(7, 5, 100.00, 'Withdraw', '2025-03-13 22:10:27', NULL),
(8, 5, 200.00, 'Withdraw', '2025-03-13 22:10:37', NULL),
(9, 6, 100.00, 'Deposit', '2025-03-13 22:20:55', NULL),
(10, 6, 100.00, 'Transfer Out', '2025-03-13 22:21:09', NULL),
(12, 7, 100.00, 'Deposit', '2025-03-13 22:23:57', NULL),
(13, 7, 100.00, 'Deposit', '2025-03-13 22:24:03', NULL),
(14, 7, 100.00, 'Transfer Out', '2025-03-13 22:24:11', NULL),
(16, 7, 500.00, 'Deposit', '2025-03-13 22:28:15', NULL),
(17, 7, 500.00, 'Deposit', '2025-03-13 22:28:24', NULL),
(18, 7, 1100.00, 'Withdraw', '2025-03-13 22:29:07', NULL),
(19, 7, 510.00, 'Transfer Out', '2025-03-13 22:31:45', NULL),
(20, 1, 510.00, 'Transfer In', '2025-03-13 22:31:45', NULL),
(21, 7, 250.00, 'Deposit', '2025-03-13 22:32:16', NULL),
(22, 7, 100.00, 'Deposit', '2025-03-13 22:32:55', NULL),
(23, 7, 200.00, 'Withdraw', '2025-03-13 22:33:26', NULL),
(24, 7, 150.00, 'Transfer Out', '2025-03-13 22:33:50', NULL),
(25, 1, 150.00, 'Transfer In', '2025-03-13 22:33:50', NULL),
(26, 9, 100.00, 'Withdraw', '2025-03-13 23:01:32', NULL),
(27, 1, 100.00, 'Deposit', '2025-03-13 23:10:55', NULL),
(28, 1, 1000.00, 'Withdraw', '2025-03-13 23:11:04', NULL),
(29, 1, 700.00, 'Withdraw', '2025-03-13 23:11:14', NULL),
(30, 1, 60.00, 'Withdraw', '2025-03-13 23:11:19', NULL),
(31, 1, 100.00, 'Withdraw', '2025-03-13 23:11:23', NULL),
(32, 1, 10000.00, 'Withdraw', '2025-03-13 23:15:14', NULL),
(33, 1, 50000.00, 'Deposit', '2025-03-13 23:15:25', NULL),
(34, 1, 100000.00, 'Withdraw', '2025-03-13 23:19:00', NULL),
(35, 1, 22.00, 'Withdraw', '2025-03-13 23:19:07', NULL),
(36, 2, 100.00, 'Withdraw', '2025-03-13 23:20:42', NULL),
(37, 2, 900.00, 'Withdraw', '2025-03-13 23:20:51', NULL),
(38, 2, 10000.00, 'Deposit', '2025-03-13 23:44:31', NULL),
(39, 2, 100.00, 'Transfer Out', '2025-03-14 00:06:27', 'Transferred to card 1111-1111-1111-1111'),
(40, 1, 100.00, 'Transfer In', '2025-03-14 00:06:27', 'Received from card 2'),
(41, 1, 10000.00, 'Deposit', '2025-03-14 00:08:22', NULL),
(42, 1, 50022.00, 'Deposit', '2025-03-14 00:08:38', NULL),
(43, 1, 100000.00, 'Deposit', '2025-03-14 00:08:44', NULL),
(44, 1, 12345.00, 'Transfer Out', '2025-03-14 00:08:53', 'Transferred to card 9999-9999-9999-9999'),
(45, 2, 12345.00, 'Transfer In', '2025-03-14 00:08:53', 'Received from card 1'),
(46, 2, 100.00, 'Deposit', '2025-03-14 00:53:57', NULL),
(47, 2, 100.00, 'Withdraw', '2025-03-14 00:54:02', NULL),
(55, 2, 150.00, 'Transfer In', '2025-03-14 11:39:28', 'Received from K*** (1234****8-98****)'),
(57, 1, 200.00, 'Transfer In', '2025-03-14 11:39:38', 'Received from K*** (1234****8-98****)'),
(61, 2, 150.00, 'Transfer In', '2025-03-14 12:08:14', 'Received from K*** (1234****8-98****)'),
(96, 1, 100.00, 'Transfer In', '2025-03-15 17:04:40', 'Received from K*** (1234****8-98****)'),
(99, 20, 100.00, 'Withdraw', '2025-03-16 15:40:21', NULL),
(100, 23, 100.00, 'Transfer Out', '2025-03-16 16:02:18', 'Transferred to L*** (9973****2-87****)'),
(102, 23, 100.00, 'Transfer Out', '2025-03-16 16:02:48', 'Transferred to K*** (1234****8-98****)'),
(104, 26, 100.00, 'Withdraw', '2025-03-16 16:25:53', NULL),
(110, 1, 100.00, 'Transfer In', '2025-03-16 16:36:45', 'Received from K*** (1234****8-98****)'),
(150, 28, 99999999.99, 'Deposit', '2025-03-16 18:37:02', NULL),
(151, 28, 99999999.99, 'Deposit', '2025-03-16 18:37:07', NULL),
(170, 11, 500.00, 'Deposit', '2025-03-20 23:32:38', NULL),
(171, 11, 500.00, 'Transfer Out', '2025-03-20 23:34:10', 'Transferred to L*** (9973****2-87****)'),
(172, 12, 500.00, 'Transfer In', '2025-03-20 23:34:10', 'Received from K*** (1234****8-98****)'),
(173, 11, 500.00, 'Withdraw', '2025-03-24 09:22:10', NULL),
(174, 11, 500.00, 'Transfer Out', '2025-03-24 09:22:35', 'Transferred to L*** (9973****2-87****)'),
(175, 12, 500.00, 'Transfer In', '2025-03-24 09:22:35', 'Received from K*** (1234****8-98****)');

--
-- Indexes for dumped tables
--

--
-- Indexes for table `carddetails`
--
ALTER TABLE `carddetails`
  ADD PRIMARY KEY (`Id`);

--
-- Indexes for table `pindetails`
--
ALTER TABLE `pindetails`
  ADD PRIMARY KEY (`Id`),
  ADD KEY `CardId` (`CardId`);

--
-- Indexes for table `transactions`
--
ALTER TABLE `transactions`
  ADD PRIMARY KEY (`Id`),
  ADD KEY `CardId` (`CardId`);

--
-- AUTO_INCREMENT for dumped tables
--

--
-- AUTO_INCREMENT for table `carddetails`
--
ALTER TABLE `carddetails`
  MODIFY `Id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=37;

--
-- AUTO_INCREMENT for table `pindetails`
--
ALTER TABLE `pindetails`
  MODIFY `Id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=122;

--
-- AUTO_INCREMENT for table `transactions`
--
ALTER TABLE `transactions`
  MODIFY `Id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=176;

--
-- Constraints for dumped tables
--

--
-- Constraints for table `pindetails`
--
ALTER TABLE `pindetails`
  ADD CONSTRAINT `pindetails_ibfk_1` FOREIGN KEY (`CardId`) REFERENCES `carddetails` (`Id`);

--
-- Constraints for table `transactions`
--
ALTER TABLE `transactions`
  ADD CONSTRAINT `transactions_ibfk_1` FOREIGN KEY (`CardId`) REFERENCES `carddetails` (`Id`);
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
