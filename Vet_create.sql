-- Created by Vertabelo (http://vertabelo.com)
-- Last modification date: 2025-04-22 18:52:47.041

-- tables
-- Table: Animals
CREATE TABLE Animals (
    ID int  NOT NULL IDENTITY,
    Name varchar(30)  NOT NULL,
    Weight decimal(5,2)  NOT NULL,
    Category varchar(30)  NOT NULL,
    CoatColor varchar(30)  NOT NULL,
    CONSTRAINT Animals_pk PRIMARY KEY  (ID)
);

-- Table: Visits
CREATE TABLE Visits (
    ID int  NOT NULL IDENTITY,
    Date datetime  NOT NULL,
    Description varchar(500)  NOT NULL,
    Price decimal(8,2)  NOT NULL,
    Animal_ID int  NOT NULL,
    CONSTRAINT Visits_pk PRIMARY KEY  (ID)
);

-- foreign keys
-- Reference: Visits_Animals (table: Visits)
ALTER TABLE Visits ADD CONSTRAINT Visits_Animals
    FOREIGN KEY (Animal_ID)
    REFERENCES Animals (ID);

-- End of file.

