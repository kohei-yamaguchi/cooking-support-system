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
-- Table structure for table `recipes`
--

DROP TABLE IF EXISTS `recipes`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
 SET character_set_client = utf8mb4 ;
CREATE TABLE `recipes` (
  `id` char(40) NOT NULL,
  `user_id` char(40) NOT NULL,
  `title` varchar(255) DEFAULT NULL,
  `description` varchar(255) DEFAULT NULL,
  `serving_for` varchar(255) DEFAULT NULL,
  `advice` varchar(255) DEFAULT NULL,
  `history` varchar(255) DEFAULT NULL,
  `published_at` date NOT NULL
) ENGINE=MyISAM DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `recipes`
--

LOCK TABLES `recipes` WRITE;
/*!40000 ALTER TABLE `recipes` DISABLE KEYS */;
INSERT INTO `recipes` VALUES ('f6d71b907d1e0c56701ba67530e463d25faa6dd9','ba9636d4509e04f695e98e61c9ecb3e765ca7055','シンプル肉じゃが','和食の定番 肉じゃがレシピ！','4人分','ジャガイモが煮えたら、強火で煮汁を煮詰めます。 薄味なので、濃い方がお好きな方は醤油を大さじ1／2くらい増やしてください。','我が家の肉じゃがの覚え書き です。','2013-12-26'),('2edba74c210544181be8cc162a882f705b5bfd14','0d47cc0c377eea8646c04633cd35bd5090bf4bd4','シンプルなハンバーグ','下味は味噌だけで、美味しく仕上がります 。\n子供が大好きなハンバーグになります。','４個分','ふわふわジューシーにするために、片栗粉を少なくし柔らかめにします。 最初に両面を強火で焼き目をつけることで柔らかくても型崩れしにくくなります。 ソースはケチャップを多くすることで子供向けになります。 ','至ってシンプルなハンバーグです。','2010-02-11'),('145271c07688e20703f67841b75ac7e2e82dffc1','de4502fe8a01706d13350390d76abe07a5f6b490','私のシンプルハンバーグ','小学生の時に始めて作ったハンバーグ。 シンプルなハンバーグが一番好きです。',NULL,'焼き過ぎないように。 ハンバーグはよくこねましょう。 タマネギは炒めず、混ぜるだけの方がおいしいです！','母がタマネギは炒めなくてもいいんだよ♪と教えてもらって、こういう作り方になりました。シンプルが一番好きデス。','2007-01-22');
/*!40000 ALTER TABLE `recipes` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2019-03-12 11:39:08
