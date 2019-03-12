CREATE DATABASE  IF NOT EXISTS `cooking_support` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci */;
USE `cooking_support`;
-- MySQL dump 10.13  Distrib 8.0.15, for Win64 (x86_64)
--
-- Host: localhost    Database: cooking_support
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
-- Table structure for table `behavior_types`
--

DROP TABLE IF EXISTS `behavior_types`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
 SET character_set_client = utf8mb4 ;
CREATE TABLE `behavior_types` (
  `behavior_type` char(40) COLLATE utf8_bin NOT NULL,
  `meaning` text COLLATE utf8_bin,
  `instruction_form` text COLLATE utf8_bin,
  `is_processed` tinyint(1) DEFAULT '0',
  `is_grasped` tinyint(1) DEFAULT '1',
  `is_collided` tinyint(1) DEFAULT '1',
  PRIMARY KEY (`behavior_type`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_bin;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `behavior_types`
--

LOCK TABLES `behavior_types` WRITE;
/*!40000 ALTER TABLE `behavior_types` DISABLE KEYS */;
INSERT INTO `behavior_types` VALUES ('broil','焼く','裏返して焼いて',1,1,1),('cut','切る','切って',1,1,1),('fry','炒める','炒めて',1,1,1),('knead','こねる','こねて',1,1,1),('mix','混ぜる','混ぜて',1,1,1),('open','開ける','開けて',1,1,0),('pour','注ぐ','注いで',0,1,0),('push','押す','押して',0,0,1),('put','置く','置いて',0,0,1),('put_in','入れる','入れて',0,0,1),('shape','形作る','形作って',1,1,0),('skim','あくを取る','あくを取って',0,1,1);
/*!40000 ALTER TABLE `behavior_types` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2019-03-12 11:39:12
