EhTagConnector
====
连接到 [EhTagTransation 数据库](https://github.com/ehtagtranslation/Database)的 RESTful API。

## API 使用

### API 域名

<https://ehtagconnector.azurewebsites.net/api/>

### 版本控制
使用 [`ETag`](https://developer.mozilla.org/zh-CN/docs/Web/HTTP/Headers/ETag) 进行版本控制，其值为最新一次 Git commit 的 sha1 值。可以使用[查询数据库数据版本](#查询数据库数据版本) API 进行查询。

+ `ETag` 将随 `HTTP 2XX` 响应返回。

+ 对于 `GET` 请求，可以使用 [`If-None-Match`](https://developer.mozilla.org/zh-CN/docs/Web/HTTP/Headers/If-None-Match) 控制缓存。
  
+ 对于 `POST`, `PUT`, `DELETE` 请求，必须使用 [`If-Match`](https://developer.mozilla.org/zh-CN/docs/Web/HTTP/Headers/If-Match) 头以防止编辑冲突。  
  
  当未包含 `If-Match` 头时，将返回 `HTTP 400 Bad Request`；  
  当 `If-Match` 头的版本与最新版本不匹配时，将返回 `HTTP 412 Precondition Failed`，此时需要使用对应的 `GET` 请求更新 `ETag` 及相应的资源。
  
> 参考：[HTTP 条件请求](https://developer.mozilla.org/zh-CN/docs/Web/HTTP/Conditional_requests)

### 用户认证

进行数据库修改（`POST`, `PUT`, `DELETE` 请求）时需要进行用户认证，需要的信息为用户的 GitHub token，可通过 [OAuth](https://developer.github.com/apps/building-oauth-apps/) 或 [PAT](https://github.com/settings/tokens) 获取，只用于确认用户信息，不需要除 public access 外的特殊 scope。

认证信息通过 [Authentication Header](https://developer.mozilla.org/zh-CN/docs/Web/HTTP/Authentication) 输入（如 `Authorization: token aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa`）。

提交的显示效果如下：  
![](/DocImages/commit.png)

### 查询数据库基本情况

路径: `GET /database`

示例请求：
```yml
GET /api/database
---
Accept: application/json
Accept-Encoding: gzip, deflate, br
If-None-Match: "5bd33aed633b18d5bca6b2d8c66dcf6b56bd75b1"
```

示例响应：
```yml
HTTP/2.0 200 OK
---
Content-Type: application/json; charset=utf-8
Content-Encoding: br
ETag: "10ee33e7a348bf5842433944baa196da53eaa0df"
```

```js
{
  //Git 地址
  "repo": "https://github.com/ehtagtranslation/Database.git",
  //最新一次提交信息
  "head": {
    "author": {
      "name": "OpportunityLiu",
      "email": "Opportunity@live.in",
      "when": "2019-02-06T14:06:00+00:00"
    },
    "committer": {
      "name": "EhTagApi-Bot",
      "email": "47353891+EhTagApi-Bot@users.noreply.github.com",
      "when": "2019-02-06T14:06:00+00:00"
    },
    "sha": "10ee33e7a348bf5842433944baa196da53eaa0df",
    "message": "In parody: Added 'gotoubun no hanayome'.\n\nPrevious value: (non-existence)\nCurrent value: | gotoubun no hanayome | 五等分的新娘 | 《五等分的新娘》（日语：五等分の花嫁）是由日本漫画家春场葱所创作的少年漫画作品。于《周刊少年Magazine》2017年第36・37合并号开始正式连载中。 | [维基百科](https://zh.wikipedia.org/zh-cn/五等分的新娘) (\\*) |\n"
  },
  //数据库结构版本
  "version": 5,
  //数据库内容摘要
  "data": [
    {
      "namespace": "reclass",
      "count": 11
    },//...
}
```

### 查询数据库数据版本

如只需获取 `ETag` 信息（即最新一次提交的 sha1），可以使用 `HEAD` 请求。

路径: `HEAD /database`

示例请求：
```yml
HEAD /api/database
---
If-None-Match: "5bd33aed633b18d5bca6b2d8c66dcf6b56bd75b1"
```

示例响应：
```yml
HTTP/2.0 204 No Content
---
ETag: "10ee33e7a348bf5842433944baa196da53eaa0df"
```


### 查询某一分类的信息

路径: `GET /database/:namespace`

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
  "count": 11
}
```

### 查询某一条目是否存在

路径: `HEAD /database/:namespace/:original`

示例请求：
```yml
HEAD /api/database/reclass/private
---
```

示例响应：
```yml
HTTP/2.0 204 No Content
---
ETag: "d4553b638098466ef013567b319c034f8ee34950"
```

> 条目不存在则返回 `HTTP 404 Not Found`。

### 查询某一条目的翻译

路径: `GET /database/:namespace/:original`

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

> 条目不存在则返回 `HTTP 404 Not Found`。

### 增加条目

路径: `POST /database/:namespace`

示例请求：
```yml
POST /api/database/parody
---
Authorization: token aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa
If-Match: "5bd33aed633b18d5bca6b2d8c66dcf6b56bd75b1"
Content-Type: application/json
```

```js
{
    "original": "gotoubun no hanayome",
    "translated": "五等分的新娘",
    "introduction": "《五等分的新娘》（日语：五等分の花嫁）是由日本漫画家春场葱所创作的少年漫画作品。于《周刊少年Magazine》2017年第36・37合并号开始正式连载中。 ",
    "externalLinks": "[维基百科](https://zh.wikipedia.org/zh-cn/五等分的新娘) (*)"
}
```

示例响应：
```yml
HTTP/2.0 201 Created
---
Content-Type: application/json; charset=utf-8
Content-Encoding: gzip
Location: api/database/parody/gotoubun no hanayome
ETag: "d4553b638098466ef013567b319c034f8ee34950"
```

```js
{
    "original": "gotoubun no hanayome",
    "translated": "五等分的新娘",
    "introduction": "《五等分的新娘》（日语：五等分の花嫁）是由日本漫画家春场葱所创作的少年漫画作品。于《周刊少年Magazine》2017年第36・37合并号开始正式连载中。 ",
    "externalLinks": "[维基百科](https://zh.wikipedia.org/zh-cn/五等分的新娘) (*)"
}
```

> 已有同名条目时将返回 `HTTP 422 Unprocessable Entity`，需改用 `PUT` 请求。

### 修改条目

路径: `PUT /database/:namespace`

示例请求：
```yml
PUT /api/database/reclass
---
Authorization: token aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa
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
ETag: "5bd33aed633b18d5bca6b2d8c66dcf6b56bd75b1"
```

```js
{
  "original": "private",
  "translated": "私人的",
  "introduction": "私人画廊是一个非正式的类别，允许用户不与 E-Hentai 社区其他成员分享他们的内容。他们往往是想要成为自己的个人用户画廊，他们只是希望自己的朋友前来参观。",
  "externalLinks": ""
}
```

> 当请求内容与数据库内容一致时（未进行修改），将返回 `HTTP 204 No Content`。

> 条目不存在则返回 `HTTP 404 Not Found`，需改用 `POST` 请求。

### 删除条目

路径: `/database/:namespace/:original`

示例请求：
```yml
DELETE /api/database/reclass/private
---
Authorization: token aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa
If-Match: "3b24693f057ccb422ce76a3334be549c66139309"
```

示例响应：
```yml
HTTP/2.0 204 No Content
---
ETag: "5bd33aed633b18d5bca6b2d8c66dcf6b56bd75b1"
```

> 条目不存在则返回 `HTTP 404 Not Found`。
