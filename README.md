EhTagConnector
====
连接到 [EhTagTransation 数据库](https://github.com/ehtagtranslation/Database)的 RESTful API。

## API 使用

### API 域名

<https://ehtagconnector.azurewebsites.net/api/>

### 版本控制
使用 [`ETag`](https://developer.mozilla.org/zh-CN/docs/Web/HTTP/Headers/ETag) 进行版本控制，其值为最新一次 Git commit 的 sha1 值。

+ 对于 `GET` 请求，`ETag` 将随 `HTTP2XX` 响应返回。
  
  可以使用 [`If-None-Match`](https://developer.mozilla.org/zh-CN/docs/Web/HTTP/Headers/If-None-Match) 控制缓存。
  
+ 对于 `POST`, `PUT`, `DELETE` 请求，必须使用 [`If-Match`](https://developer.mozilla.org/zh-CN/docs/Web/HTTP/Headers/If-Match) 头以防止编辑冲突。  
  
  当未包含 `If-Match` 头时，将返回 `HTTP400`；当 `If-Match` 头的版本与最新版本不匹配时，将返回 `HTTP412`，此时需要使用对应的 `GET` 请求更新 `ETag` 及相应的资源。

### 用户授权

进行数据库修改（`POST`, `PUT`, `DELETE` 请求）时需要进行用户授权，需要的信息为用户名 (`username`) 和邮箱 (`email`)，通过 URL Query 输入（如 `POST /api/database/reclass/gamecg?username=USER&email=user@example.com`）。

为了便于将修改对应到相应的 GitHub 用户，建议使用 GitHub 用户名和相应的注册邮箱。

### 查询 API (`GET` 请求)

#### 数据库基本情况

路径: `/database`

示例请求：
```yml
GET /api/database
---
Accept: application/json
Accept-Encoding: gzip, deflate, br
```

示例响应：
```yml
HTTP/2.0 200 OK
---
Content-Type: application/json; charset=utf-8
Content-Encoding: br
ETag: "d4553b638098466ef013567b319c034f8ee34950"
```

```js
{
  //Git 地址
  "repo": "https://github.com/ehtagtranslation/Database.git",
  //最新一次提交信息
  "head": {
    "author": {
      "name": "Opportunity",
      "email": "Opportunity@live.in",
      "when": "2019-02-06T02:14:52+08:00"
    },
    "committer": {
      "name": "GitHub",
      "email": "noreply@github.com",
      "when": "2019-02-06T02:14:52+08:00"
    },
    "sha": "d4553b638098466ef013567b319c034f8ee34950",
    "message": "Update LICENSE.md"
  },
  //数据库结构版本
  "version": 5,
  //数据库内容摘要
  "data": [
    {
      "namespace": "reclass",
      "count": 11
    }, // ...
  ]
}
```

#### 某一分类的翻译

路径: `/database/:namespace`

示例请求：
```yml
GET /api/database/reclass
---
Accept: application/json
Accept-Encoding: gzip, deflate, br
```

示例响应：
```yml
HTTP/2.0 200 OK
---
Content-Type: application/json; charset=utf-8
Content-Encoding: br
ETag: "d4553b638098466ef013567b319c034f8ee34950"
```

```js
{
  "namespace": "reclass",
  "count": 11,
  "data": [
    {
      "original": "gamecg",
      "translated": "游戏CG集",
      "introduction": "计算机生成的作品，往往从工口游戏（色情游戏）提取。一般有大量图像。并不是指的视频游戏中的人物图像或游戏视频游戏截图。",
      "externalLinks": ""
    },//...
  ]
}
```

#### 某一条目的翻译

路径: `/database/:namespace/:original`

示例请求：
```yml
GET /api/database/reclass/private
---
Accept: application/json
Accept-Encoding: gzip, deflate, br
```

示例响应：
```yml
HTTP/2.0 200 OK
---
Content-Type: application/json; charset=utf-8
Content-Encoding: br
ETag: "d4553b638098466ef013567b319c034f8ee34950"
```

```js
{
  "original": "private",
  "translated": "私人的",
  "introduction": "私人画廊是一个非正式的类别，允许用户不与 E-Hentai 社区其他成员分享他们的内容。他们往往是想要成为自己的个人用户画廊，他们只是希望自己的朋友前来参观。",
  "externalLinks": ""
}
```

### 增加 API (`POST` 请求)

#### 增加条目

路径: `/database/:namespace`

示例请求：
```yml
POST /api/database/reclass?username=OpportunityLiu&email=Opportuniy@live.in
---
If-Match: "5dd45269a56960affbcd13cd3828f6dbe577341f"
Content-Type: application/json
```

```js
{
  "original": "private",
  "translated": "私人的",
  "introduction": "私人画廊是一个非正式的类别，允许用户不与 E-Hentai 社区其他成员分享他们的内容。他们往往是想要成为自己的个人用户画廊，他们只是希望自己的朋友前来参观。",
  "externalLinks": ""
}
```

示例响应：
```yml
HTTP/2.0 201 Created
---
Content-Type: application/json; charset=utf-8
Content-Encoding: gzip
Location: api/database/reclass/private
```

```js
{
  "original": "private",
  "translated": "私人的",
  "introduction": "私人画廊是一个非正式的类别，允许用户不与 E-Hentai 社区其他成员分享他们的内容。他们往往是想要成为自己的个人用户画廊，他们只是希望自己的朋友前来参观。",
  "externalLinks": ""
}
```

### 修改 API (`PUT` 请求)

#### 修改条目

路径: `/database/:namespace`

示例请求：
```yml
PUT /api/database/reclass?username=OpportunityLiu&email=Opportuniy@live.in
---
Content-Type: application/json
If-Match: "d4553b638098466ef013567b319c034f8ee34950"
```

```js
{
  "original": "private",
  "translated": "私人的",
  "introduction": "私人画廊是一个非正式的类别，允许用户不与 E-Hentai 社区其他成员分享他们的内容。他们往往是想要成为自己的个人用户画廊，他们只是希望自己的朋友前来参观。",
  "externalLinks": ""
}
```

示例响应：
```yml
HTTP/2.0 200 OK
---
Content-Type: application/json; charset=utf-8
Content-Encoding: gzip
```

```js
{
  "original": "private",
  "translated": "私人的",
  "introduction": "私人画廊是一个非正式的类别，允许用户不与 E-Hentai 社区其他成员分享他们的内容。他们往往是想要成为自己的个人用户画廊，他们只是希望自己的朋友前来参观。",
  "externalLinks": ""
}
```

### 删除 API (`DELETE` 请求)

#### 删除条目

路径: `/database/:namespace/:original`

示例请求：
```yml
DELETE /api/database/reclass/private?username=OpportunityLiu&email=Opportuniy@live.in
---
If-Match: "3b24693f057ccb422ce76a3334be549c66139309"
```

示例响应：
```yml
HTTP/2.0 204 No Content
---
```