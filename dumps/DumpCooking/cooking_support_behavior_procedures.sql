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
-- Table structure for table `behavior_procedures`
--

DROP TABLE IF EXISTS `behavior_procedures`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
 SET character_set_client = utf8mb4 ;
CREATE TABLE `behavior_procedures` (
  `expert_id` int(11) NOT NULL,
  `recipe_id` char(40) COLLATE utf8_bin NOT NULL,
  `step` int(11) NOT NULL,
  `procedure_number` int(11) NOT NULL,
  `grasped_objects` text COLLATE utf8_bin,
  `colliding_objects` text COLLATE utf8_bin,
  `behavior_type` text COLLATE utf8_bin,
  PRIMARY KEY (`recipe_id`,`step`,`procedure_number`,`expert_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_bin;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `behavior_procedures`
--

LOCK TABLES `behavior_procedures` WRITE;
/*!40000 ALTER TABLE `behavior_procedures` DISABLE KEYS */;
INSERT INTO `behavior_procedures` VALUES (1,'145271c07688e20703f67841b75ac7e2e82dffc1',1,1,'ingredient2','ingredient2,cutting_board','put'),(1,'145271c07688e20703f67841b75ac7e2e82dffc1',1,2,'kitchen_knife','kitchen_knife,ingredient2,cutting_board','cut'),(1,'145271c07688e20703f67841b75ac7e2e82dffc1',1,3,'ingredient1','ingredient1,bowl','put_in'),(1,'145271c07688e20703f67841b75ac7e2e82dffc1',1,4,'ingredient3','ingredient3,ingredient1,bowl','put_in'),(1,'145271c07688e20703f67841b75ac7e2e82dffc1',1,5,'processed_food1','processed_food1,ingredient3,bowl','put_in'),(1,'145271c07688e20703f67841b75ac7e2e82dffc1',1,6,'ingredient5','ingredient5,processed_food1,bowl','put_in'),(1,'145271c07688e20703f67841b75ac7e2e82dffc1',1,7,'ingredient9','ingredient9,ingredient5,bowl','put_in'),(1,'145271c07688e20703f67841b75ac7e2e82dffc1',1,8,'ingredient4','ingredient4,ingredient9,bowl','put_in'),(1,'145271c07688e20703f67841b75ac7e2e82dffc1',1,9,'ingredient6','ingredient6,ingredient4,bowl','put_in'),(1,'145271c07688e20703f67841b75ac7e2e82dffc1',1,10,'ingredient7','ingredient7,ingredient6,bowl','put_in'),(1,'145271c07688e20703f67841b75ac7e2e82dffc1',1,11,'bowl','EthanRightHand,ingredient7,bowl','knead'),(1,'145271c07688e20703f67841b75ac7e2e82dffc1',2,1,'processed_food2,processed_food2','processed_food2,EthanLeftHand,EthanRightHand','shape'),(1,'145271c07688e20703f67841b75ac7e2e82dffc1',2,2,'ingredient8','ingredient8,frying_pan','put_in'),(1,'145271c07688e20703f67841b75ac7e2e82dffc1',2,3,'processed_food3','processed_food3,ingredient8,frying_pan','put_in'),(1,'145271c07688e20703f67841b75ac7e2e82dffc1',2,4,'frying_pan,chopsticks','chopsticks,processed_food3,frying_pan','broil'),(1,'145271c07688e20703f67841b75ac7e2e82dffc1',2,5,'lid','lid,processed_food4,frying_pan','put'),(1,'145271c07688e20703f67841b75ac7e2e82dffc1',3,1,'lid','lid,processed_food4,pan','open'),(1,'145271c07688e20703f67841b75ac7e2e82dffc1',3,2,'processed_food5','processed_food5,dish','put'),(1,'145271c07688e20703f67841b75ac7e2e82dffc1',3,3,'ingredient10','ingredient10,processed_food5,dish','put'),(1,'f6d71b907d1e0c56701ba67530e463d25faa6dd9',1,1,'ingredient7','ingredient7,cutting_board','put'),(1,'f6d71b907d1e0c56701ba67530e463d25faa6dd9',1,2,'kitchen_knife','kitchen_knife,ingredient7,cutting_board','cut'),(1,'f6d71b907d1e0c56701ba67530e463d25faa6dd9',2,1,'ingredient6','ingredient6,cutting_board','put'),(1,'f6d71b907d1e0c56701ba67530e463d25faa6dd9',2,2,'kitchen_knife','kitchen_knife,ingredient6,cutting_board','cut'),(1,'f6d71b907d1e0c56701ba67530e463d25faa6dd9',3,1,'ingredient4','ingredient4,cutting_board','put'),(1,'f6d71b907d1e0c56701ba67530e463d25faa6dd9',3,2,'kitchen_knife','kitchen_knife,ingredient4,cutting_board','cut'),(1,'f6d71b907d1e0c56701ba67530e463d25faa6dd9',4,1,'ingredient5','ingredient5,cutting_board','put'),(1,'f6d71b907d1e0c56701ba67530e463d25faa6dd9',4,2,'kitchen_knife','kitchen_knife,ingredient5,cutting_board','cut'),(1,'f6d71b907d1e0c56701ba67530e463d25faa6dd9',5,1,'ingredient3','ingredient3,pan','put_in'),(1,'f6d71b907d1e0c56701ba67530e463d25faa6dd9',5,2,'processed_food1','processed_food1,ingredient3,pan','put_in'),(1,'f6d71b907d1e0c56701ba67530e463d25faa6dd9',5,3,'pan,chopsticks','chopsticks,processed_food1,pan','fry'),(1,'f6d71b907d1e0c56701ba67530e463d25faa6dd9',5,4,'processed_food3','processed_food3,processed_food5,pan','put_in'),(1,'f6d71b907d1e0c56701ba67530e463d25faa6dd9',5,5,'processed_food2','processed_food2,processed_food3,pan','put_in'),(1,'f6d71b907d1e0c56701ba67530e463d25faa6dd9',5,6,'pan,chopsticks','chopsticks,processed_food2,pan','fry'),(1,'f6d71b907d1e0c56701ba67530e463d25faa6dd9',5,7,'ingredient10','ingredient10,processed_food6,pan','put_in'),(1,'f6d71b907d1e0c56701ba67530e463d25faa6dd9',5,8,'ingredient8','ingredient8,ingredient10,pan','put_in'),(1,'f6d71b907d1e0c56701ba67530e463d25faa6dd9',5,9,'spoon','spoon,ingredient8,pan','skim'),(1,'f6d71b907d1e0c56701ba67530e463d25faa6dd9',5,10,'ingredient1','ingredient1,ingredient8,pan','put_in'),(1,'f6d71b907d1e0c56701ba67530e463d25faa6dd9',5,11,'ingredient2','ingredient2,ingredient1,pan','put_in'),(1,'f6d71b907d1e0c56701ba67530e463d25faa6dd9',5,12,'ingredient9','ingredient9,ingredient2,pan','put_in'),(1,'f6d71b907d1e0c56701ba67530e463d25faa6dd9',5,13,'processed_food4','processed_food4,ingredient9,pan','put_in'),(1,'f6d71b907d1e0c56701ba67530e463d25faa6dd9',5,14,'lid','lid,processed_food4,pan','put'),(1,'f6d71b907d1e0c56701ba67530e463d25faa6dd9',6,1,'lid','lid,processed_food4,pan','open'),(1,'f6d71b907d1e0c56701ba67530e463d25faa6dd9',6,2,'processed_food7','processed_food7,ricebowl','put_in');
/*!40000 ALTER TABLE `behavior_procedures` ENABLE KEYS */;
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
