# 📱 MecaRage Android App — Architecture Plan

## Overview

Android app (Java) targeting **Clients** and **Mechanics**, connected to the existing
ASP.NET Core backend (MySQL) with Firebase used for real-time delivery and push notifications.

---

## 🏗️ Chosen Architecture: REST-First + Firebase for Real-Time

### Why NOT "Firebase RTDB as primary data store → sync to backend"

| Issue | Detail |
|-------|--------|
| **No free Cloud Functions** | Writing triggers that call the backend requires Firebase Blaze (paid) |
| **Sync complexity** | Bidirectional sync between Firebase RTDB and MySQL is a nightmare (conflicts, ordering, partial failures) |
| **Double source of truth** | Same data lives in two places — bugs, stale data, inconsistencies |
| **Offline edge cases** | What if the phone writes to RTDB but the Cloud Function never fires? Data silently lost in MySQL |

### ✅ Chosen: REST-First + Firebase for Real-Time Push

```
Android App (Java)
      │
      ├─── Retrofit ──────────────────▶  ASP.NET Core API ──▶ MySQL
      │    (CRUD: appointments,           (single source of truth)
      │     symptoms, invoices,                    │
      │     interventions...)                       │ Firebase Admin SDK
      │                                             │ (writes realtime events)
      ├─── Firebase Auth ──────────────▶            ▼
      │    (login/register)            Firebase Realtime DB
      │                                      │
      └─── Firebase RTDB listener ◀──────────┘
           (live status updates,       FCM ──────────────────▶ Android Push
            notifications)
```

**Data flow rules:**
- **Any write** (create appointment, submit report, approve invoice) → goes to REST API → MySQL
- **Backend events** (status changes, new notifications) → backend writes a lightweight event to Firebase RTDB
- **Android app** listens to its own Firebase RTDB path `/users/{userId}/events/*` for real-time UI updates
- **Push notifications** when app is in background → FCM via Firebase Admin SDK on backend

This way MySQL is always the single source of truth and Firebase is purely a delivery channel.

---

## 📂 Android Project Structure

```
android-app/
├── app/
│   └── src/main/
│       ├── java/com/mecarage/app/
│       │   ├── MainActivity.java
│       │   ├── auth/
│       │   │   ├── LoginActivity.java
│       │   │   └── RegisterActivity.java
│       │   ├── client/
│       │   │   ├── ClientDashboardActivity.java
│       │   │   ├── AppointmentsActivity.java
│       │   │   ├── InterventionsActivity.java
│       │   │   ├── InvoicesActivity.java
│       │   │   └── NotificationsActivity.java
│       │   ├── mechanic/
│       │   │   ├── MechanicDashboardActivity.java
│       │   │   ├── TasksActivity.java
│       │   │   └── ExaminationActivity.java
│       │   ├── api/
│       │   │   ├── ApiClient.java         ← Retrofit singleton
│       │   │   ├── ApiService.java        ← all REST endpoints
│       │   │   └── AuthInterceptor.java   ← adds JWT header
│       │   ├── firebase/
│       │   │   ├── FirebaseAuthManager.java
│       │   │   └── RealtimeEventListener.java
│       │   ├── models/
│       │   │   ├── LoginRequest.java / LoginResponse.java
│       │   │   ├── AppointmentDto.java
│       │   │   ├── InterventionDto.java
│       │   │   ├── InvoiceDto.java
│       │   │   └── NotificationDto.java
│       │   ├── adapters/
│       │   │   ├── AppointmentsAdapter.java
│       │   │   ├── InterventionsAdapter.java
│       │   │   └── NotificationsAdapter.java
│       │   ├── services/
│       │   │   └── MecaFirebaseMessagingService.java  ← FCM handler
│       │   └── utils/
│       │       ├── SessionManager.java    ← stores JWT in SharedPreferences
│       │       └── NetworkUtils.java
│       ├── res/
│       │   ├── layout/
│       │   │   ├── activity_login.xml
│       │   │   ├── activity_register.xml
│       │   │   ├── activity_client_dashboard.xml
│       │   │   ├── activity_mechanic_dashboard.xml
│       │   │   ├── activity_appointments.xml
│       │   │   ├── activity_interventions.xml
│       │   │   └── item_*.xml             ← RecyclerView row layouts
│       │   └── values/
│       │       ├── strings.xml
│       │       └── colors.xml
│       └── google-services.json           ← from Firebase console
└── build.gradle
```

---

## 🔐 Authentication Flow

### Login (Email + Password)

```
User enters email/password
        │
        ▼
Firebase Auth.signInWithEmailAndPassword()
        │
        ▼
Firebase ID Token (short-lived, ~1 hour)
        │
        ▼
POST /api/auth/firebase-signin   ← new backend endpoint
  Body: { firebaseIdToken: "..." }
        │
        ▼ Backend: verifies token via Firebase Admin SDK
          finds/creates user in MySQL
          returns backend JWT
        │
        ▼
SessionManager.saveToken(jwt)   ← stored in SharedPreferences
        │
        ▼
All subsequent Retrofit calls add:  Authorization: Bearer {jwt}
```

### Register (New Client)

```
User fills: firstName, lastName, email, password, phone
        │
        ▼
Firebase Auth.createUserWithEmailAndPassword()
        │
        ▼ (on success, get Firebase UID)
POST /api/auth/register-firebase
  Body: { firebaseUid, email, firstName, lastName, phone, role: "Client" }
        │
        ▼ Backend creates user in MySQL, links Firebase UID
        ▼ Returns backend JWT
```

---

## 🔔 Real-Time Notifications Flow

### Backend → Android (when app is OPEN)

```
1. Any backend event fires (e.g., invoice finalized, car ready)
2. Backend NotificationService writes to Firebase RTDB:
   /users/{userId}/notifications/{notifId} = {
     title, message, type, entityId, createdAt, isRead: false
   }
3. Android ValueEventListener on /users/{userId}/notifications/
   → updates UI in real time (no polling needed)
```

### Backend → Android (when app is in BACKGROUND/CLOSED)

```
1. Same backend event fires
2. Backend also calls FCM via Firebase Admin SDK:
   FirebaseMessaging.getInstance().send(Message.builder()
     .setToken(deviceFcmToken)
     .setNotification(Notification.builder()
       .setTitle(title)
       .setBody(message)
       .build())
     .build());
3. Android receives it as a system push notification
4. Tapping it opens the relevant Activity
```

### Android → Backend (FCM token registration)

```
Android: MyFirebaseMessagingService.onNewToken(token)
        │
        ▼
POST /api/users/fcm-token   { fcmToken: "..." }
        │
        ▼
Backend: stores FCM token on User entity
```

---

## 📱 Screens by Role

### CLIENT

| Screen | Data Source | Description |
|--------|-------------|-------------|
| Login | Firebase Auth + `/api/auth/firebase-signin` | Standard login |
| Register | Firebase Auth + `/api/auth/register-firebase` | New client |
| Dashboard | Firebase RTDB (live) | Active intervention status card + unread notif count |
| Mesappointments | `GET /api/appointments/my` | List past and upcoming appointments |
| Book Appointment | `POST /api/appointments` | Form with vehicle, date, symptoms |
| Mes Interventions | `GET /api/interventions/lifecycle/my` | Lifecycle timeline cards |
| Invoice Detail | `GET /api/invoices/{id}` | Approve/reject + PDF download |
| Notifications | Firebase RTDB `/users/{uid}/notifications` | Real-time list |

### MECHANIC

| Screen | Data Source | Description |
|--------|-------------|-------------|
| Login | Firebase Auth + `/api/auth/firebase-signin` | Same login flow |
| Dashboard | `GET /api/repair-tasks/my` + Firebase live badge | Task list with status |
| Examination Form | `POST /api/repair-tasks/{id}/submit-examination` | Submit findings + parts |
| Repair Completion | `PATCH /api/repair-tasks/{id}/submit-repair` | Mark repair done |
| Notifications | Firebase RTDB `/users/{uid}/notifications` | Live assignment alerts |

---

## 🔧 Backend Changes Required

### New: Firebase Admin SDK (NuGet)

```
FirebaseAdmin (Google's official .NET package)
```

### New endpoint: `POST /api/auth/firebase-signin`

Validates Firebase ID token → returns backend JWT. Allows users
registered on web (Angular) to log in on mobile without re-registering.

### New endpoint: `POST /api/auth/register-firebase`

Creates user in MySQL and links Firebase UID. Called from Android during signup.

### New endpoint: `POST /api/users/fcm-token`

Stores the device FCM token on the authenticated user so backend can send push notifs.

### Modified: `NotificationService` (or `NotificationCommandHandler`)

After saving a notification to MySQL → also write to Firebase RTDB at
`/users/{userId}/notifications/{id}` and send FCM push if user has a token.

### New: `FirebaseService.cs` (Infrastructure layer)

```csharp
public interface IFirebaseService
{
    Task WriteNotificationAsync(string userId, object notification);
    Task SendPushAsync(string fcmToken, string title, string body);
}
```

---

## 🗃️ Firebase RTDB Structure

```json
{
  "users": {
    "{userId}": {
      "notifications": {
        "{notificationId}": {
          "title": "Votre véhicule est prêt",
          "message": "Vous pouvez venir récupérer votre voiture",
          "type": "VehicleReady",
          "entityId": "intervention-uuid",
          "createdAt": 1746480000000,
          "isRead": false
        }
      },
      "interventionStatus": {
        "{interventionId}": {
          "status": "ReadyForPickup",
          "updatedAt": 1746480000000
        }
      }
    }
  }
}
```

**Firebase RTDB Security Rules:**
```json
{
  "rules": {
    "users": {
      "$uid": {
        ".read": "auth != null && auth.uid == $uid",
        ".write": "auth != null && auth.uid == $uid"
      }
    }
  }
}
```

---

## 📦 Android Dependencies (build.gradle)

```groovy
// Networking
implementation 'com.squareup.retrofit2:retrofit:2.11.0'
implementation 'com.squareup.retrofit2:converter-gson:2.11.0'
implementation 'com.squareup.okhttp3:logging-interceptor:4.12.0'

// Firebase
implementation platform('com.google.firebase:firebase-bom:33.7.0')
implementation 'com.google.firebase:firebase-auth'
implementation 'com.google.firebase:firebase-database'
implementation 'com.google.firebase:firebase-messaging'

// Image loading
implementation 'com.github.bumptech.glide:glide:4.16.0'

// UI
implementation 'com.google.android.material:material:1.12.0'
implementation 'androidx.swiperefreshlayout:swiperefreshlayout:1.1.0'
```

---

## 🗺️ Implementation Phases

### Phase 1 — Foundation (new Android project + auth)
- [ ] Create Android Studio project (Java, min SDK 26 / Android 8)
- [ ] Firebase project setup (Auth + RTDB + FCM enabled)
- [ ] Add `google-services.json` to app
- [ ] `LoginActivity.java` + `RegisterActivity.java` with Firebase Auth
- [ ] Backend: add `FirebaseAdmin` NuGet, add `firebase-service-account.json`
- [ ] Backend: `POST /api/auth/firebase-signin` + `POST /api/auth/register-firebase`
- [ ] `SessionManager.java` (JWT storage in SharedPreferences)
- [ ] `ApiClient.java` + `AuthInterceptor.java` (Retrofit)

### Phase 2 — Client Screens
- [ ] `ClientDashboardActivity` with active intervention card
- [ ] `AppointmentsActivity` (list + book new)
- [ ] `InterventionsActivity` (lifecycle timeline)
- [ ] `InvoicesActivity` (view + approve/reject)

### Phase 3 — Mechanic Screens
- [ ] `MechanicDashboardActivity` with task list
- [ ] `ExaminationActivity` (form: findings + parts)
- [ ] `RepairCompletionActivity`

### Phase 4 — Real-Time
- [ ] Backend: `FirebaseService.cs` writes to RTDB on every notification create
- [ ] Backend: `POST /api/users/fcm-token` endpoint
- [ ] Android: `RealtimeEventListener.java` (ValueEventListener on user path)
- [ ] Android: `MecaFirebaseMessagingService.java` (FCM background push handler)
- [ ] Badge update on dashboard when new notification arrives

### Phase 5 — Polish
- [ ] Offline mode indicator
- [ ] Pull-to-refresh on all lists
- [ ] PDF invoice view (WebView or Intent to browser)
- [ ] Splash screen + app icon

---

## ⚙️ Firebase Project Setup (one-time)

1. Go to https://console.firebase.google.com → **New Project** → name it `MecaRage`
2. **Authentication** → Sign-in method → Enable **Email/Password**
3. **Realtime Database** → Create database → Start in **locked mode** → paste security rules above
4. **Cloud Messaging** → enabled by default
5. **Project Settings** → **Service accounts** → Generate new private key → save as
   `backend/MecaManage.API/firebase-service-account.json` (**never commit this file**)
6. **Project Settings** → **Your apps** → Add Android → package name `com.mecarage.app`
   → download `google-services.json` → place in `android-app/app/`

---

## 🛡️ Security Notes

- `firebase-service-account.json` must be added to `.gitignore` immediately
- `google-services.json` contains no secrets (safe to commit)
- Backend JWT secret and Firebase service account are different credentials
- Firebase RTDB rules ensure users can only read/write their own path
- FCM tokens are per-device; a user can have multiple (multiple Android devices)

---

## Ready to Start?

Recommended first step: **Phase 1** — Create the Android Studio project and wire up the login screen.

Run the following to create the project structure and all Phase 1 files.

