# CHANGELOG

<!--- next entry here -->

## 1.2.6-develop.1
2022-03-07

### Fixes

- add commit signing and assembly version to build (f4471c947a7c50a11dc8cf976b105fb5c4dfdcd6)

## 1.2.5
2021-08-18

### Fixes

- set GetObjectResponse DataStream to position 0 before returning to caller (06fa6ef949b384b2f23bd216179f70e269ce3a21)

## 1.2.5-develop.2
2021-08-18

### Fixes

- set GetObjectResponse DataStream to position 0 before returning to caller (06fa6ef949b384b2f23bd216179f70e269ce3a21)

## 1.2.4
2021-07-14

### Fixes

- minor package updates (8dbb11f725dc40c5cf34266c66ff100fed0a4847)
- remove explicit AssemblyVersion during build (79d0efc1a1725cc4e58d8b018fe12e2921bfc755)
- empty commit to trigger new main branch [ci-skip] (0aff9a9d9e452d0e322c2f89a96db735f47788ec)

## 1.2.4-develop.1
2021-07-13

### Fixes

- minor package updates (8dbb11f725dc40c5cf34266c66ff100fed0a4847)
- remove explicit AssemblyVersion during build (79d0efc1a1725cc4e58d8b018fe12e2921bfc755)

## 1.2.3
2021-01-28

### Fixes

- correction for response handling for deferred functions when the web server middleware prevents the request ([a46519a](https://gitlab.com/ionburst/ionburst-sdk-net/commit/a46519a7536cae19dcfac56b361cdda374c033d6))

## 1.2.2
2021-01-22

### Fixes

- removed break; because of unintended consequence ([f16c290](https://gitlab.com/ionburst/ionburst-sdk-net/commit/f16c2909ed074316a6c6533a0c4a2f870ece46d2))

## 1.2.1
2021-01-22

### Fixes

- break; on wrong line ([107ae06](https://gitlab.com/ionburst/ionburst-sdk-net/commit/107ae0611c50c48b4d0aad31070a4e251f4920cd))

## 1.2.0
2021-01-22

### Features

- support for api/Secrets methods that were added to Ionburst api plus some internal class restructuring ([0d87fc6](https://gitlab.com/ionburst/ionburst-sdk-net/commit/0d87fc635b71f5346ddd69a3d21ccbadd481b6d2))

## 1.1.4
2020-11-13

### Fixes

- put activity GUID in response of successful GET (if API provides it) ([8f93f2b](https://gitlab.com/ionburst/ionburst-sdk-net/commit/8f93f2b92177ddaa51d055f6b578b4ae0e775741))

## 1.1.3
2020-08-17

### Fixes

- Missed marking CheckIonBurstAPI as obsolete ([27a6a90](https://gitlab.com/ionburst/ionburst-sdk-net/commit/27a6a9068b3c282cc8447df54d805ec8a54b63fd))

## 1.1.2
2020-08-17

### Fixes

- Added GetConfiguredUri function and changed a few IonBurst to Ionburst ([f45bcf3](https://gitlab.com/ionburst/ionburst-sdk-net/commit/f45bcf3e52758bbc5033f73a8d0dd3c816fee4ef))

## 1.1.1
2020-07-30

### Fixes

- Making sure SDK handles older API ([95cdbf6](https://gitlab.com/ionburst/ionburst-sdk-net/commit/95cdbf69fce9cc38d6efbbdd2c48b026f7492efa))

## 1.1.0
2020-07-29

### Features

- re-designed classification functionality and new function to get upload size limit ([4207a54](https://gitlab.com/ionburst/ionburst-sdk-net/commit/4207a540c305e6c2ae03928fca793c45f31644a0))

## 1.0.1
2020-05-13

## 1.0.0
2020-02-11

### Fixes

- handle changed API response from POST/GET /deferred/start ([4620089](https://gitlab.com/ionburst/ionburst-sdk-net/commit/46200898468924670c56dcd619737d84a2992611))

### Fixes

- Add CI file ready for 1.0.0 release ([1129715](https://gitlab.com/ionburst/ionburst-sdk-net/commit/1129715aa9163e5080236ababee8d15b062bd774))