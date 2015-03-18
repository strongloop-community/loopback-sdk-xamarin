#THIS EXAMPLE IS DEPRECATED

An updated version is available at [loopback-example-model-relations](https://github.com/strongloop/loopback-example-model-relations)

---

#loopback-example-relations-basic

- [Overview](#overview)
- [Prerequisites](#prerequisites)
- [Example](#example)
  - [Getting started](#1-getting-started)
  - [Create the models](#2-create-the-models)
  - [Create the front-end](#3-create-the-front-end)
  - [Add sample data](#4-add-sample-data)
  - [Create model relations](#5-create-model-relations)
  - [Try the API](#6-try-the-api)
  - [Conclusion](#7-conclusion)

#Overview
The purpose of this example is to demonstrate model relations in [LoopBack](http://loopback.io). We will create a web application to demonstrate model relations. The main page will consist of various links that allow us to query and filter data through an exposed REST API.

#Prerequisites
This guide assumes you have working knowledge of:
- [LoopBack Database Connectors](https://github.com/strongloop/loopback-example-database)
- [LoopBack Models](http://docs.strongloop.com/display/LB/Working+with+models)

You should also have the following installed:
- [Node.js](http://nodejs.org/)
- [NPM](http://www.npmjs.com/)
- [StrongLoop Controller](http://strongloop.com/get-started/) `npm install -g strongloop`

#Example

##1. Getting started
Scaffold a new application by running `slc loopback loopback-example-relations-basic`. You should see:

```
? What's the name of your application? (loopback-example-relations-basic)
```

Press <enter> to accept the name. Once the project has finished scaffolding, you
should see a generated dir named `loopback-example-relations-basic`. `cd` into
this dir (which we'll refer to as the *project root* from hereon).

##2. Create the models
We will be using an in-memory database to hold our data. [Create a model](http://docs.strongloop.com/display/LB/Creating+models) named `Customer` by running:

```shell
cd loopback-example-relation
slc loopback:model Customer
```

You should see:

```shell
? Enter the model name: Customer
? Select the data-source to attach Customer to: db (memory)
? Select model's base class: PersistedModel
? Expose Customer via the REST API? Yes
? Custom plural form (used to build REST URL):
Let's add some Customer properties now.

Enter an empty property name when done.
? Property name: name
   invoke   loopback:property
? Property type: string
? Required? No

Let's add another Customer property.
Enter an empty property name when done.
? Property name: age
   invoke   loopback:property
? Property type: number
? Required? No

Let's add another Customer property.
Enter an empty property name when done.
? Property name: #leave blank, press enter
```

Follow the prompts to finish creating the model. Repeat for `Review` and `Order` using the following properties:
- Review
  - product:string
  - star:number
- Order
  - description:string
  - total:number

> You should see `customer.json`, `order.json` and `review.json` in `common/models` when you're done.

##3. Create the front-end
Let's create a front-end to make it easier to analyze our data.

###Install EJS
From the project root, run `npm install --save ejs`.

###Serve `index.html`
Modify [`server/server.js`](https://github.com/strongloop/loopback-example-relations-basic/blob/master/server/server.js#L11-L14) to serve `index.html`.

###Create the `views` dir
From the project root, run `mkdir -p server/views`.

###Create `index.html`
Inside the [`views` directory](https://github.com/strongloop/loopback-example-relations-basic/tree/master/server/views), create [`index.html`](https://github.com/strongloop/loopback-example-relations-basic/blob/master/server/views/index.html).

You can view what we have so far by executing `slc run server` from the project root and browsing to [localhost:3000](http://localhost:3000). Click on [API Explorer](http://localhost:3000/explorer) and you will notice that the models we created from [step 2](#2-create-the-models) are there.

> You may also notice some of the API endpoints return empty arrays or errors. It's because the database is empty. In addition, we need to define model relations for some of the API endpoints to work. Don't fret, we'll get to all that very soon!

##4. Add sample data
In `server/boot`, create the following boot scripts:

- [`create-customers.js`](server/boot/create-customers.js)
- [`create-reviews.js`](server/boot/create-reviews.js)
- [`create-orders.js`](server/boot/create-orders.js)

Each script will be run upon application startup and will load the its
corresponding sample data.

> `automigrate()` recreates the database table/index if it already exists. In other words, existing tables will be dropped and ALL EXISTING DATA WILL BE LOST. For more information, see the [documentation](http://apidocs.strongloop.com/loopback-datasource-juggler/#datasourceautomigratemodel-callback).

> `Model.scope...` is only in `create-customers.js`.

##5. Create model relations
From the project root, run:

```shell
slc loopback:relation
```

Follow the prompts and create the following relationships:
- Customer
  - has many
    - Review
      - property name for the relation: reviews
      - custom foreign key: authorId
    - Order
      - property name for the relation: orders
      - custom foreign key: customerId

---
- Review
  - belongs to
    - Customer
      - property name for the relation: author
      - custom foreign key: authorId

---
- Order
  - belongs to
    - Customer

> For any item without *property name for the relation* or *custom foreign key*, just use the defaults. LoopBack will [derive](http://docs.strongloop.com/display/LB/BelongsTo+relations#BelongsTorelations-Overview) these values automatically when you don't specify one.

When you're done, your `common/models/customer.json` should look like:

```json
{
  "name": "Customer",
  "base": "PersistedModel",
  "properties": {
    "name": {
      "type": "string"
    },
    "age": {
      "type": "number"
    }
  },
  "validations": [],
  "relations": {
    "reviews": {
      "type": "hasMany",
      "model": "Review",
      "foreignKey": "authorId"
    },
    "orders": {
      "type": "hasMany",
      "model": "Order",
      "foreignKey": "customerId"
    }
  },
  "acls": [],
  "methods": []
}

```

`common/models/reviews.json` should look like:

```json
{
  "name": "Review",
  "base": "PersistedModel",
  "properties": {
    "product": {
      "type": "string"
    },
    "star": {
      "type": "number"
    }
  },
  "validations": [],
  "relations": {
    "author": {
      "type": "belongsTo",
      "model": "Customer",
      "foreignKey": "authorId"
    }
  },
  "acls": [],
  "methods": []
}
```

and `common/models/order.json` should look like:

```json
{
  "name": "Order",
  "base": "PersistedModel",
  "properties": {
    "description": {
      "type": "string"
    },
    "total": {
      "type": "number"
    }
  },
  "validations": [],
  "relations": {
    "customer": {
      "type": "belongsTo",
      "model": "Customer",
      "foreignKey": ""
    }
  },
  "acls": [],
  "methods": []
}
```

> You should be creating four relations in total: *Customer has many Reviews*, *Customer has many Orders*, *Review belongs to Customer* and *Order belongs to Customer*.

##6. Try the API
Restart application (`slc run server` in case you forgot) and browse to [localhost:3000](http://localhost:3000). Each endpoint should be working properly now that we've defined the model relations. See the following endpoint descriptions:

- [/api/customers](http://localhost:3000/api/customers)
  - List all customers

---

- [/api/customers?filter[fields][0]=name](http://localhost:3000/api/customers?filter[fields][0]=name)
  - List all customers, but only return the name property for each result

---

- [/api/customers/1](http://localhost:3000/api/customers/1)
  - Look up a customer by ID

---

- [/api/customers/youngFolks](http://localhost:3000/api/customers/youngFolks)
  - List a predefined scope named *youngFolks*

---

- [/api/customers/1/reviews](http://localhost:3000/api/customers/1/reviews)
  - List all reviews posted by a given customer

---

- [/api/customers/1/orders](http://localhost:3000/api/customers/1/orders)
  - List all orders placed by a given customer

---

- [/api/customers?filter[include]=reviews](http://localhost:3000/api/customers?filter[include]=reviews)
  - List all customers including their reviews

---

- [/api/customers?filter[include][reviews]=author](http://localhost:3000/api/customers?filter[include][reviews]=author)
  - List all customers including their reviews which also include the author

---

- [/api/customers?filter[include][reviews]=author&filter[where][age]=21](http://localhost:3000/api/customers?filter[include][reviews]=author&filter[where][age]=21)
  - List all customers whose age is 21, including their reviews which also include the author

---

- [/api/customers?filter[include][reviews]=author&filter[limit]=2](http://localhost:3000/api/customers?filter[include][reviews]=author&filter[limit]=2)
  - List first two customers including their reviews which also include the author

---

- [/api/customers?filter[include]=reviews&filter[include]=orders](http://localhost:3000/api/customers?filter[include]=reviews&filter[include]=orders)
  - List all customers including their reviews and orders

##7. Conclusion
That's it! You've successfully created an application with models formed into a complex data graph! For a deeper dive into relations, see this [blog entry](http://strongloop.com/strongblog/defining-and-mapping-data-relations-with-loopback-connected-models/) (the examples were written for LoopBack 1.x, but the concepts still apply). If you have further questions, please refer to the [LoopBack documentation](http://docs.strongloop.com/display/LB/LoopBack+2.0).
