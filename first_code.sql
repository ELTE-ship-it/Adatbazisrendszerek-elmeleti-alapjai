-- Sequence törlése, ha már létezik
DROP SEQUENCE transaction_seq;

-- Táblák törlése, ha már léteznek
DROP TABLE transaction_queue CASCADE CONSTRAINTS;
DROP TABLE versioned_data CASCADE CONSTRAINTS;
DROP TABLE key_access_frequency CASCADE CONSTRAINTS;
DROP TABLE transaction_conflicts CASCADE CONSTRAINTS;
DROP TABLE deadlock_logs CASCADE CONSTRAINTS;
DROP TABLE hot_data_cache CASCADE CONSTRAINTS;
DROP TABLE transaction_reads CASCADE CONSTRAINTS;
DROP TABLE transaction_writes CASCADE CONSTRAINTS;
SET SERVEROUTPUT ON;

CREATE SEQUENCE transaction_seq START WITH 1 INCREMENT BY 1;

CREATE TABLE transaction_queue (
    transaction_id NUMBER PRIMARY KEY,  
    priority NUMBER DEFAULT 0,          
    status VARCHAR2(20) DEFAULT 'pending',  
    start_time TIMESTAMP,
    end_time TIMESTAMP,
    age NUMBER DEFAULT 0,  
    urgency NUMBER DEFAULT 1  
);

CREATE TABLE versioned_data (
    key_name VARCHAR2(255),
    value VARCHAR2(255),
    version NUMBER DEFAULT 1,
    last_updated TIMESTAMP,
    PRIMARY KEY (key_name, version)
);

CREATE TABLE key_access_frequency (
    key_name VARCHAR2(255),
    access_count NUMBER DEFAULT 0,
    last_accessed TIMESTAMP,
    PRIMARY KEY (key_name)
);

CREATE TABLE transaction_conflicts (
    conflict_id NUMBER PRIMARY KEY, 
    transaction_id NUMBER,
    conflicting_key VARCHAR2(255),
    conflict_time TIMESTAMP
);

CREATE TABLE deadlock_logs (
    transaction_id NUMBER,
    resolution_time TIMESTAMP
);

CREATE TABLE hot_data_cache (
    key_name VARCHAR2(255) PRIMARY KEY,
    value VARCHAR2(255),
    last_updated TIMESTAMP
);

CREATE TABLE transaction_reads (
    transaction_id NUMBER,
    key_name VARCHAR2(255),
    PRIMARY KEY (transaction_id, key_name)
);

CREATE TABLE transaction_writes (
    transaction_id NUMBER,
    key_name VARCHAR2(255),
    PRIMARY KEY (transaction_id, key_name)
);

-- Prioritás számítása és életkor alapú prioritás
CREATE OR REPLACE PROCEDURE calculate_transaction_priority (t_id IN NUMBER) IS
    key_name VARCHAR2(255);
    total_conflicts NUMBER DEFAULT 0;
    total_accesses NUMBER DEFAULT 0;
    priority NUMBER DEFAULT 0;
BEGIN
    -- A tranzakció által használt kulcsok feldolgozása
    FOR rec IN (SELECT key_name FROM versioned_data WHERE key_name IN 
               (SELECT key_name FROM transaction_reads WHERE transaction_id = t_id
               UNION ALL 
               SELECT key_name FROM transaction_writes WHERE transaction_id = t_id)) LOOP
        
        -- Kulcs-hozzáférési gyakoriság és konfliktusok számítása
        SELECT access_count INTO total_accesses FROM key_access_frequency WHERE key_name = rec.key_name;
        SELECT COUNT(*) INTO total_conflicts FROM transaction_conflicts WHERE conflicting_key = rec.key_name;

        -- Növeljük a prioritást
        priority := priority + total_conflicts + total_accesses;
    END LOOP;

    -- Frissítjük a tranzakció prioritását
    UPDATE transaction_queue SET priority = priority + priority WHERE transaction_id = t_id;
END;
/

-- Életkor alapú prioritás frissítése
CREATE OR REPLACE PROCEDURE update_transaction_priorities IS
BEGIN
    -- Az életkor növelése és a prioritás frissítése
    UPDATE transaction_queue
    SET age = age + 1, 
        priority = priority - (age * urgency)
    WHERE status = 'pending';
END;
/

-- Konfliktusok kezelése
CREATE OR REPLACE PROCEDURE proactive_conflict_detection IS
    conflicting_tid NUMBER;
    conflict_probability NUMBER;
BEGIN
    FOR rec IN (SELECT t1.transaction_id, COUNT(*) AS conflict_count
                FROM transaction_queue t1
                JOIN transaction_queue t2
                ON t1.status = 'pending' AND t2.status = 'running'
                WHERE t1.transaction_id != t2.transaction_id
                AND EXISTS (
                    SELECT 1 FROM transaction_writes tw1
                    JOIN transaction_writes tw2
                    ON tw1.key_name = tw2.key_name
                    WHERE tw1.transaction_id = t1.transaction_id
                    AND tw2.transaction_id = t2.transaction_id)
                GROUP BY t1.transaction_id) LOOP
        
        -- Konfliktus valószín?ség növelése
        UPDATE transaction_queue
        SET priority = priority + rec.conflict_count * 10
        WHERE transaction_id = rec.transaction_id;
    END LOOP;
END;
/

-- Batch feldolgozás dinamikus mérettel
CREATE OR REPLACE PROCEDURE process_transaction (t_id IN NUMBER) IS
    transaction_priority NUMBER;
    transaction_status VARCHAR2(20); 
    trans_start_time TIMESTAMP;
BEGIN
    -- Tranzakció információ lekérése
    SELECT priority, status, start_time 
    INTO transaction_priority, transaction_status, trans_start_time 
    FROM transaction_queue 
    WHERE transaction_id = t_id;

    -- Csak akkor dolgozzuk fel, ha a státusz 'pending'
    IF transaction_status = 'pending' THEN
        -- A tranzakció státusza 'running'-re vált
        UPDATE transaction_queue 
        SET status = 'running', start_time = SYSDATE 
        WHERE transaction_id = t_id;

        -- Szimuláljuk az olvasási/írási m?veleteket a versioned_data táblából
        UPDATE versioned_data 
        SET value = 'updated_by_trans_' || t_id,  -- Sztring összef?zés
            version = version + 1, 
            last_updated = SYSDATE
        WHERE key_name = 'key1';  -- Példa kulcs

        -- A tranzakció státusza 'committed'-re vált
        UPDATE transaction_queue 
        SET status = 'committed', end_time = SYSDATE 
        WHERE transaction_id = t_id;

        -- Üzenet visszaadása PL/SQL kifejezéssel
        DBMS_OUTPUT.PUT_LINE('Transaction ' || t_id || ' has been committed successfully.');
    ELSE
        DBMS_OUTPUT.PUT_LINE('Transaction ' || t_id || ' is already processed.');
    END IF;
END;
/


-- Deadlock felismerés és feloldás
CREATE OR REPLACE PROCEDURE detect_and_resolve_deadlocks IS
BEGIN
    -- Deadlock megoldása a legalacsonyabb prioritású tranzakció megszakításával
    UPDATE transaction_queue
    SET status = 'aborted'
    WHERE transaction_id = (SELECT transaction_id FROM transaction_queue ORDER BY priority DESC FETCH FIRST 1 ROW ONLY);
    
    -- Deadlock esemény naplózása
    INSERT INTO deadlock_logs (transaction_id, resolution_time) 
    VALUES ((SELECT transaction_id FROM transaction_queue ORDER BY priority DESC FETCH FIRST 1 ROW ONLY), SYSDATE);
END;
/

-- Insert initial data into versioned_data and key_access_frequency
INSERT INTO versioned_data (key_name, value, version, last_updated) VALUES ('key1', 'value1', 1, SYSDATE);
INSERT INTO versioned_data (key_name, value, version, last_updated) VALUES ('key2', 'value2', 1, SYSDATE);
INSERT INTO key_access_frequency (key_name, access_count, last_accessed) VALUES ('key1', 5, SYSDATE);
INSERT INTO key_access_frequency (key_name, access_count, last_accessed) VALUES ('key2', 3, SYSDATE);

-- Insert multiple transactions into transaction_queue
INSERT INTO transaction_queue (transaction_id, priority, status, start_time, end_time, age, urgency)
VALUES (1, 0, 'pending', NULL, NULL, 0, 1);
INSERT INTO transaction_queue (transaction_id, priority, status, start_time, end_time, age, urgency)
VALUES (2, 0, 'pending', NULL, NULL, 0, 1);
INSERT INTO transaction_queue (transaction_id, priority, status, start_time, end_time, age, urgency)
VALUES (3, 0, 'pending', NULL, NULL, 0, 1);
INSERT INTO transaction_queue (transaction_id, priority, status, start_time, end_time, age, urgency)
VALUES (4, 0, 'pending', NULL, NULL, 0, 1);

-- Simulate read/write operations to create conflicts
INSERT INTO transaction_reads (transaction_id, key_name) VALUES (1, 'key1');
INSERT INTO transaction_writes (transaction_id, key_name) VALUES (1, 'key2');

INSERT INTO transaction_reads (transaction_id, key_name) VALUES (2, 'key1');
INSERT INTO transaction_writes (transaction_id, key_name) VALUES (2, 'key2');

INSERT INTO transaction_reads (transaction_id, key_name) VALUES (3, 'key2');
INSERT INTO transaction_writes (transaction_id, key_name) VALUES (3, 'key1');

INSERT INTO transaction_reads (transaction_id, key_name) VALUES (4, 'key1');
INSERT INTO transaction_writes (transaction_id, key_name) VALUES (4, 'key2');

-- Calculate priorities based on current state
EXECUTE calculate_transaction_priority(1);
EXECUTE calculate_transaction_priority(2);
EXECUTE calculate_transaction_priority(3);
EXECUTE calculate_transaction_priority(4);

-- Print current state of transaction_queue for review
SET SERVEROUTPUT ON;
BEGIN
    FOR rec IN (SELECT * FROM transaction_queue) LOOP
        DBMS_OUTPUT.PUT_LINE('Transaction ID: ' || rec.transaction_id || ', Priority: ' || rec.priority || ', Status: ' || rec.status);
    END LOOP;
END;
/

-- Update transaction priorities based on age
EXECUTE update_transaction_priorities;

-- Print updated state of transaction_queue
BEGIN
    FOR rec IN (SELECT * FROM transaction_queue) LOOP
        DBMS_OUTPUT.PUT_LINE('Updated Transaction ID: ' || rec.transaction_id || ', Priority: ' || rec.priority || ', Status: ' || rec.status);
    END LOOP;
END;
/

-- Simulate proactive conflict detection
EXECUTE proactive_conflict_detection;

-- Detect and resolve deadlocks if necessary
EXECUTE detect_and_resolve_deadlocks;

-- Final state check
BEGIN
    FOR rec IN (SELECT * FROM deadlock_logs) LOOP
        DBMS_OUTPUT.PUT_LINE('Deadlock resolved for Transaction ID: ' || rec.transaction_id || ' at ' || rec.resolution_time);
    END LOOP;
END;
/
