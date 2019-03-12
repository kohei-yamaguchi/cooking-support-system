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
-- Table structure for table `steps`
--

DROP TABLE IF EXISTS `steps`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
 SET character_set_client = utf8mb4 ;
CREATE TABLE `steps` (
  `recipe_id` char(40) NOT NULL,
  `position` int(11) DEFAULT NULL,
  `memo` text
) ENGINE=MyISAM DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `steps`
--

LOCK TABLES `steps` WRITE;
/*!40000 ALTER TABLE `steps` DISABLE KEYS */;
INSERT INTO `steps` VALUES ('8f95726c9cf6c0fc23b38b8a858b0318d631b770',1,'強力粉と小麦粉(薄力粉） をまぜる。ぬるま湯のなかに、サラダオイル・はちみつ・卵をいれ混ぜた後、粉の2/3を ふるいにかけながら少しづつ混ぜる。最初は水っぽいのでへらで混ぜる。'),('f6d71b907d1e0c56701ba67530e463d25faa6dd9',6,'ジャガイモに火が通ったら、火を強めて煮汁を煮詰めたら出来上がり！'),('2edba74c210544181be8cc162a882f705b5bfd14',1,'豚ひき肉を白っぽくなるまでよくこねます。 玉ねぎ・卵・味噌・片栗粉を加えてよくこねて、４等分にします。'),('2edba74c210544181be8cc162a882f705b5bfd14',2,'熱したフライパンに油をひき、形を整えた１を強火で両面焼きます。'),('2edba74c210544181be8cc162a882f705b5bfd14',3,'中火にして蓋をして蒸し焼きにします。 中まで火が通ったら取り出します。'),('2edba74c210544181be8cc162a882f705b5bfd14',4,'フライパンに☆を入れて、煮詰めてソースを作ります。 ３にかけて出来上がりです。'),('f6d71b907d1e0c56701ba67530e463d25faa6dd9',5,'鍋にごま油を熱し、肉、玉ねぎ、ジャガイモの順に炒めます。 ☆を入れ煮立ったら、しらたきを入れ、弱～中火で煮ます。'),('145271c07688e20703f67841b75ac7e2e82dffc1',1,'①たまねぎはみじんぎり。 ②ボウルにお肉とタマネギ、牛乳・卵・パン粉・塩コショウ・ナツメグ・オールスパイスをいれてよくねります～。'),('145271c07688e20703f67841b75ac7e2e82dffc1',3,'⑤ソースをかけて出来上がり。'),('145271c07688e20703f67841b75ac7e2e82dffc1',2,'③ハンバーグの形にします。 ④フライパンにバターを熱して、ハンバーグをいれ両面よく焼きます。真ん中はへこませてね！最初は強火で焦げ目をつけ、あとは弱火でフタをしてね。'),('f6d71b907d1e0c56701ba67530e463d25faa6dd9',4,'しらたきは食べやすい長さに切ります。'),('f6d71b907d1e0c56701ba67530e463d25faa6dd9',2,'ジャガイモを大きめに乱切りします。'),('f6d71b907d1e0c56701ba67530e463d25faa6dd9',3,'玉ねぎは大きめにスライスします。'),('f6d71b907d1e0c56701ba67530e463d25faa6dd9',1,'肉（牛、豚、鳥のどれでも美味しい）を一口大にします。');
/*!40000 ALTER TABLE `steps` ENABLE KEYS */;
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
