# 📄 SRS – Socket Student System

---

# 1. Giới thiệu

## 1.1 Mục đích

Tài liệu này mô tả các yêu cầu phần mềm cho hệ thống **Socket Student System**, một ứng dụng client–server sử dụng giao thức TCP, cho phép:

* kết nối server qua socket
* cấu hình kết nối cơ sở dữ liệu SQL Server
* nhập và gửi dữ liệu sinh viên
* mã hóa dữ liệu bằng DES
* lưu trữ vào database
* truy xuất, giải mã và tính điểm trung bình

---

## 1.2 Phạm vi

Hệ thống bao gồm:

* **Client (WPF)**:

  * giao diện người dùng
  * gửi request đến server
  * hiển thị kết quả

* **Server (Console)**:

  * xử lý TCP
  * xử lý nghiệp vụ
  * kết nối SQL Server
  * mã hóa DES
  * lưu và truy xuất dữ liệu

---

## 1.3 Định nghĩa

| Thuật ngữ   | Ý nghĩa                    |
| ----------- | -------------------------- |
| TCP         | Giao thức truyền thông     |
| DES         | Thuật toán mã hóa đối xứng |
| DTO         | Data Transfer Object       |
| MessageType | Loại message client-server |
| Envelope    | cấu trúc message JSON      |

---

# 2. Tổng quan hệ thống

## 2.1 Kiến trúc

Hệ thống sử dụng mô hình:

* **Client–Server**
* giao tiếp qua **TCP Socket**
* dữ liệu truyền bằng **JSON + Length-prefix**

---

## 2.2 Luồng tổng thể

```text
Client → TCP Connect → Server
Client → DbConnect → Server → SQL Server
Client → StudentAdd → Server → DB
Client → ResultsGet → Server → DB → decrypt → compute → Client
```

---

# 3. UI/UX Thiết kế

---

# 3.1 Màn hình 1 – Kết nối TCP Server

## Mục tiêu

Thiết lập kết nối giữa client và server.

---

## UI Layout

```text
Server Address: [__________]
Port:           [____]

[ Connect ]

Status: Disconnected
```

---

## Thành phần

| Field          | Mô tả             |
| -------------- | ----------------- |
| Server Address | IP hoặc hostname  |
| Port           | Cổng TCP          |
| Connect Button | Thực hiện kết nối |
| Status Label   | Trạng thái        |

---

## Giá trị mặc định

* Server Address: `localhost`
* Port: `9000`

---

## Validation

* Address: không rỗng
* Port:

  * là số
  * 1–65535

---

## Trạng thái UI

| State      | Hành vi       |
| ---------- | ------------- |
| Initial    | enable input  |
| Connecting | disable input |
| Connected  | khóa input    |
| Failed     | enable retry  |

---

## Behavior

* Khi click **Connect**:

  * validate input
  * gọi TCP connect
  * timeout 3–5s
* Nếu thành công:

  * hiển thị "Connected"
  * enable step DB
* Nếu thất bại:

  * hiển thị lỗi
  * cho retry

---

---

# 3.2 Màn hình 2 – Kết nối Database (Simplified từ DBeaver)

## Mục tiêu

Cho phép user nhập thông tin DB và gửi lên server.

---

## UI Layout

```text
SQL Server Address: [__________]
Port:               [____]
Database Name:      [__________]

Username:           [__________]
Password:           [__________]

[ Connect Database ]

Status: Not Connected
```

---

## Thiết kế

* Lấy cảm hứng từ **DBeaver**
* Đã tối giản:

  * bỏ SSL
  * bỏ driver
  * bỏ URL

---

## Validation

| Field    | Rule     |
| -------- | -------- |
| Server   | required |
| Port     | number   |
| Database | required |
| Username | required |
| Password | required |

---

## Behavior

* Chỉ enable khi TCP connected
* Khi click:

  * gửi message `DbConnect`
* Server phản hồi:

  * `DbConnectOk`
  * `DbConnectFail`

---

## Trạng thái

| State         | UI               |
| ------------- | ---------------- |
| Not Connected | default          |
| Connecting    | disable button   |
| Connected     | enable step tiếp |
| Failed        | show error       |

---

---

# 4. Functional Requirements

---

## 4.1 Client

### FR-C01 – TCP Connection

Client phải cho phép nhập địa chỉ server và port để kết nối TCP.

---

### FR-C02 – Database Connection

Client phải cho phép nhập thông tin DB và gửi message `DbConnect`.

---

### FR-C03 – Gửi dữ liệu sinh viên

Client gửi từng sinh viên bằng `StudentAdd`.

---

### FR-C04 – Lấy kết quả

Client gửi `ResultsGet` với:

* mode = ALL
* mode = BY_ID

---

### FR-C05 – Hiển thị kết quả

Client hiển thị:

* họ tên
* mã sinh viên
* điểm trung bình

---

---

## 4.2 Server

### FR-S01 – TCP Server

Server phải nhận nhiều client đồng thời.

---

### FR-S02 – Xử lý message

Server xử lý theo `MessageType`.

---

### FR-S03 – Kết nối DB

Server nhận `DbConnect` và kết nối SQL Server.

---

### FR-S04 – Validate dữ liệu

Server kiểm tra dữ liệu sinh viên.

---

### FR-S05 – Mã hóa DES

Server mã hóa:

* FullName
* Math
* Literature
* English

---

### FR-S06 – Lưu DB

* Insert record
* StudentId unique

---

### FR-S07 – Giải mã và tính toán

* decrypt
* tính average

---

### FR-S08 – Trả kết quả

Trả:

* FullName
* StudentId
* AverageScore

---

---

# 5. Communication Protocol

---

## 5.1 Framing

* 4-byte length prefix
* UTF-8 JSON

---

## 5.2 Message Envelope

```json
{
  "type": "...",
  "requestId": "...",
  "timestamp": "...",
  "payload": {}
}
```

---

## 5.3 Message Types

| Type        | Direction       |
| ----------- | --------------- |
| DbConnect   | Client → Server |
| DbConnectOk | Server → Client |
| StudentAdd  | Client → Server |
| ResultsGet  | Client → Server |
| Results     | Server → Client |

---

---

# 6. Database Design

---

## Table: Students

| Column     | Type      |
| ---------- | --------- |
| StudentId  | NVARCHAR  |
| FullName   | VARBINARY |
| Math       | VARBINARY |
| Literature | VARBINARY |
| English    | VARBINARY |

---

## Quy tắc

* StudentId unique
* dữ liệu được mã hóa DES
* không lưu plaintext

---

---

# 7. Business Rules

---

## 7.1 Điểm số

* 0–10
* average:

```
(Toan + Van + Anh) / 3
```

* làm tròn 2 chữ số

---

## 7.2 Mã hóa

* DES
* key nằm ở server
* không gửi cho client

---

---

# 8. Non-functional Requirements

---

## 8.1 Performance

* hỗ trợ nhiều client

---

## 8.2 Security

* dữ liệu mã hóa DES

---

## 8.3 Reliability

* xử lý lỗi TCP
* xử lý disconnect

---

---

# 9. Luồng chính

---

## Flow 1 – Kết nối

```text
Client → Connect → Server
```

---

## Flow 2 – DB Connect

```text
Client → DbConnect → Server → SQL
```

---

## Flow 3 – Add Student

```text
Client → StudentAdd → Server → Encrypt → DB
```

---

## Flow 4 – Get Results

```text
Client → ResultsGet → Server → Decrypt → Compute → Client
```

---

---

# 10. Acceptance Criteria

---

## AC-01

Client kết nối TCP thành công

## AC-02

Client gửi DB info và server kết nối được SQL

## AC-03

Server lưu dữ liệu đã mã hóa

## AC-04

Server tính đúng điểm trung bình

## AC-05

Client hiển thị đúng kết quả

---

---

# 11. Assumptions & Constraints

---

## Assumptions

* SQL Server đã tồn tại
* network ổn định

---

## Constraints

* sử dụng DES theo yêu cầu môn học
* không dùng TLS

