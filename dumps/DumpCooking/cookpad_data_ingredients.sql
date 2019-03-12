CREATE DATABASE  IF NOT EXISTS `cookpad_data` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci */;
USE `cookpad_data`;
-- MySQL dump 10.13  Distrib 8.0.15, for Win64 (x86_64)
--
-- Host: localhost    Database: cookpad_data
-- ------------------------------------------------------
-- Server version	8.0.15

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
 SET NAMES utf8 ;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `ingredients`
--

DROP TABLE IF EXISTS `ingredients`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
 SET character_set_client = utf8mb4 ;
CREATE TABLE `ingredients` (
  `recipe_id` char(40) COLLATE utf8_bin NOT NULL,
  `name` varchar(255) COLLATE utf8_bin DEFAULT NULL,
  `quantity` varchar(255) COLLATE utf8_bin DEFAULT NULL
) ENGINE=MyISAM DEFAULT CHARSET=utf8 COLLATE=utf8_bin;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `ingredients`
--

LOCK TABLES `ingredients` WRITE;
/*!40000 ALTER TABLE `ingredients` DISABLE KEYS */;
INSERT INTO `ingredients` VALUES ('2edba74c210544181be8cc162a882f705b5bfd14','豚ひき肉','１/２パック'),('2edba74c210544181be8cc162a882f705b5bfd14','玉ねぎ','１/４個'),('2edba74c210544181be8cc162a882f705b5bfd14','卵','１個'),('2edba74c210544181be8cc162a882f705b5bfd14','味噌','大さじ１'),('2edba74c210544181be8cc162a882f705b5bfd14','片栗粉','小さじ１'),('2edba74c210544181be8cc162a882f705b5bfd14','☆ケチャップ','大さじ１'),('2edba74c210544181be8cc162a882f705b5bfd14','☆ソース','大さじ１'),('2edba74c210544181be8cc162a882f705b5bfd14','☆砂糖','小さじ１'),('2edba74c210544181be8cc162a882f705b5bfd14','☆水','大さじ２'),('145271c07688e20703f67841b75ac7e2e82dffc1','牛ひき肉','200ｇ'),('145271c07688e20703f67841b75ac7e2e82dffc1','タマネギ','１個'),('145271c07688e20703f67841b75ac7e2e82dffc1','豚ひき肉','100ｇ'),('145271c07688e20703f67841b75ac7e2e82dffc1','パン粉','大３'),('145271c07688e20703f67841b75ac7e2e82dffc1','牛乳','100ｃｃ'),('145271c07688e20703f67841b75ac7e2e82dffc1','塩・こしょう・ナツメグ','少々'),('145271c07688e20703f67841b75ac7e2e82dffc1','オールスパイス（あれば）','少々'),('145271c07688e20703f67841b75ac7e2e82dffc1','無塩バター','適量'),('145271c07688e20703f67841b75ac7e2e82dffc1','卵','1個'),('145271c07688e20703f67841b75ac7e2e82dffc1','ケチャップ・中濃ソース','適量'),('f6d71b907d1e0c56701ba67530e463d25faa6dd9','☆醤油','大さじ2'),('f6d71b907d1e0c56701ba67530e463d25faa6dd9','☆みりん','大さじ1'),('f6d71b907d1e0c56701ba67530e463d25faa6dd9','ごま油','大さじ1'),('f6d71b907d1e0c56701ba67530e463d25faa6dd9','玉ねぎ','1／2個'),('f6d71b907d1e0c56701ba67530e463d25faa6dd9','しらたき','適量'),('f6d71b907d1e0c56701ba67530e463d25faa6dd9','じゃがいも','中4～5個'),('f6d71b907d1e0c56701ba67530e463d25faa6dd9','肉（今回は豚肉）','適量'),('f6d71b907d1e0c56701ba67530e463d25faa6dd9','☆酒','大さじ2'),('f6d71b907d1e0c56701ba67530e463d25faa6dd9','☆砂糖','大さじ1'),('f6d71b907d1e0c56701ba67530e463d25faa6dd9','☆水','150cc');
/*!40000 ALTER TABLE `ingredients` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2019-03-12 11:39:09
