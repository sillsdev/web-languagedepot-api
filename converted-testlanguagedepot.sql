-- Converted by db_converter
START TRANSACTION;
SET standard_conforming_strings=off;
SET escape_string_warning=off;
SET CONSTRAINTS ALL DEFERRED;

DROP TABLE IF EXISTS "ar_internal_metadata";
CREATE TABLE "ar_internal_metadata" (
    "key" varchar(510) NOT NULL,
    "value" varchar(510) DEFAULT NULL,
    "created_at" timestamp with time zone NOT NULL,
    "updated_at" timestamp with time zone NOT NULL,
    PRIMARY KEY ("key")
);

INSERT INTO "ar_internal_metadata" VALUES ('environment','production_languagedepot','2019-10-04 14:08:08','2019-10-04 14:08:08');
DROP TABLE IF EXISTS "attachments";
CREATE TABLE "attachments" (
    "id" integer NOT NULL,
    "container_id" integer DEFAULT NULL,
    "container_type" varchar(60) DEFAULT NULL,
    "filename" varchar(510) NOT NULL DEFAULT '',
    "disk_filename" varchar(510) NOT NULL DEFAULT '',
    "filesize" bigint NOT NULL DEFAULT '0',
    "content_type" varchar(510) DEFAULT '',
    "digest" varchar(128) NOT NULL DEFAULT '',
    "downloads" integer NOT NULL DEFAULT '0',
    "author_id" integer NOT NULL DEFAULT '0',
    "created_on" timestamp with time zone DEFAULT NULL,
    "description" varchar(510) DEFAULT NULL,
    "disk_directory" varchar(510) DEFAULT NULL,
    PRIMARY KEY ("id")
);

DROP TABLE IF EXISTS "auth_sources";
CREATE TABLE "auth_sources" (
    "id" integer NOT NULL,
    "type" varchar(60) NOT NULL DEFAULT '',
    "name" varchar(120) NOT NULL DEFAULT '',
    "host" varchar(120) DEFAULT NULL,
    "port" integer DEFAULT NULL,
    "account" varchar(510) DEFAULT NULL,
    "account_password" varchar(510) DEFAULT '',
    "base_dn" varchar(510) DEFAULT NULL,
    "attr_login" varchar(60) DEFAULT NULL,
    "attr_firstname" varchar(60) DEFAULT NULL,
    "attr_lastname" varchar(60) DEFAULT NULL,
    "attr_mail" varchar(60) DEFAULT NULL,
    "onthefly_register" boolean NOT NULL DEFAULT false,
    "tls" boolean NOT NULL DEFAULT false,
    "filter" text ,
    "timeout" integer DEFAULT NULL,
    "verify_peer" boolean NOT NULL DEFAULT true,
    PRIMARY KEY ("id")
);

DROP TABLE IF EXISTS "boards";
CREATE TABLE "boards" (
    "id" integer NOT NULL,
    "project_id" integer NOT NULL,
    "name" varchar(510) NOT NULL DEFAULT '',
    "description" varchar(510) DEFAULT NULL,
    "position" integer DEFAULT NULL,
    "topics_count" integer NOT NULL DEFAULT '0',
    "messages_count" integer NOT NULL DEFAULT '0',
    "last_message_id" integer DEFAULT NULL,
    "parent_id" integer DEFAULT NULL,
    PRIMARY KEY ("id")
);

DROP TABLE IF EXISTS "changes";
CREATE TABLE "changes" (
    "id" integer NOT NULL,
    "changeset_id" integer NOT NULL,
    "action" varchar(2) NOT NULL DEFAULT '',
    "path" text NOT NULL,
    "from_path" text ,
    "from_revision" varchar(510) DEFAULT NULL,
    "revision" varchar(510) DEFAULT NULL,
    "branch" varchar(510) DEFAULT NULL,
    PRIMARY KEY ("id")
);

DROP TABLE IF EXISTS "changeset_parents";
CREATE TABLE "changeset_parents" (
    "changeset_id" integer NOT NULL,
    "parent_id" integer NOT NULL
);

DROP TABLE IF EXISTS "changesets";
CREATE TABLE "changesets" (
    "id" integer NOT NULL,
    "repository_id" integer NOT NULL,
    "revision" varchar(510) NOT NULL,
    "committer" varchar(510) DEFAULT NULL,
    "committed_on" timestamp with time zone NOT NULL,
    "comments" text ,
    "commit_date" date DEFAULT NULL,
    "scmid" varchar(510) DEFAULT NULL,
    "user_id" integer DEFAULT NULL,
    PRIMARY KEY ("id"),
    UNIQUE ("repository_id","revision")
);

DROP TABLE IF EXISTS "changesets_issues";
CREATE TABLE "changesets_issues" (
    "changeset_id" integer NOT NULL,
    "issue_id" integer NOT NULL,
    UNIQUE ("changeset_id","issue_id")
);

DROP TABLE IF EXISTS "comments";
CREATE TABLE "comments" (
    "id" integer NOT NULL,
    "commented_type" varchar(60) NOT NULL DEFAULT '',
    "commented_id" integer NOT NULL DEFAULT '0',
    "author_id" integer NOT NULL DEFAULT '0',
    "content" text ,
    "created_on" timestamp with time zone NOT NULL,
    "updated_on" timestamp with time zone NOT NULL,
    PRIMARY KEY ("id")
);

DROP TABLE IF EXISTS "custom_field_enumerations";
CREATE TABLE "custom_field_enumerations" (
    "id" integer NOT NULL,
    "custom_field_id" integer NOT NULL,
    "name" varchar(510) NOT NULL,
    "active" boolean NOT NULL DEFAULT true,
    "position" integer NOT NULL DEFAULT '1',
    PRIMARY KEY ("id")
);

DROP TABLE IF EXISTS "custom_fields";
CREATE TABLE "custom_fields" (
    "id" integer NOT NULL,
    "type" varchar(60) NOT NULL DEFAULT '',
    "name" varchar(60) NOT NULL DEFAULT '',
    "field_format" varchar(60) NOT NULL DEFAULT '',
    "possible_values" text ,
    "regexp" varchar(510) DEFAULT '',
    "min_length" integer DEFAULT NULL,
    "max_length" integer DEFAULT NULL,
    "is_required" boolean NOT NULL DEFAULT false,
    "is_for_all" boolean NOT NULL DEFAULT false,
    "is_filter" boolean NOT NULL DEFAULT false,
    "position" integer DEFAULT NULL,
    "searchable" int4 DEFAULT '0',
    "default_value" text ,
    "editable" int4 DEFAULT '1',
    "visible" boolean NOT NULL DEFAULT true,
    "multiple" int4 DEFAULT '0',
    "format_store" text ,
    "description" text ,
    PRIMARY KEY ("id")
);

DROP TABLE IF EXISTS "custom_fields_projects";
CREATE TABLE "custom_fields_projects" (
    "custom_field_id" integer NOT NULL DEFAULT '0',
    "project_id" integer NOT NULL DEFAULT '0',
    UNIQUE ("custom_field_id","project_id")
);

DROP TABLE IF EXISTS "custom_fields_roles";
CREATE TABLE "custom_fields_roles" (
    "custom_field_id" integer NOT NULL,
    "role_id" integer NOT NULL,
    UNIQUE ("custom_field_id","role_id")
);

DROP TABLE IF EXISTS "custom_fields_trackers";
CREATE TABLE "custom_fields_trackers" (
    "custom_field_id" integer NOT NULL DEFAULT '0',
    "tracker_id" integer NOT NULL DEFAULT '0',
    UNIQUE ("custom_field_id","tracker_id")
);

DROP TABLE IF EXISTS "custom_values";
CREATE TABLE "custom_values" (
    "id" integer NOT NULL,
    "customized_type" varchar(60) NOT NULL DEFAULT '',
    "customized_id" integer NOT NULL DEFAULT '0',
    "custom_field_id" integer NOT NULL DEFAULT '0',
    "value" text ,
    PRIMARY KEY ("id")
);

DROP TABLE IF EXISTS "documents";
CREATE TABLE "documents" (
    "id" integer NOT NULL,
    "project_id" integer NOT NULL DEFAULT '0',
    "category_id" integer NOT NULL DEFAULT '0',
    "title" varchar(510) NOT NULL DEFAULT '',
    "description" text ,
    "created_on" timestamp with time zone DEFAULT NULL,
    PRIMARY KEY ("id")
);

DROP TABLE IF EXISTS "email_addresses";
CREATE TABLE "email_addresses" (
    "id" integer NOT NULL,
    "user_id" integer NOT NULL,
    "address" varchar(510) NOT NULL,
    "is_default" boolean NOT NULL DEFAULT false,
    "notify" boolean NOT NULL DEFAULT true,
    "created_on" timestamp with time zone NOT NULL,
    "updated_on" timestamp with time zone NOT NULL,
    PRIMARY KEY ("id")
);

INSERT INTO "email_addresses" VALUES (1,1,'admin@example.net',1,1,'2009-07-22 06:32:07','2009-07-23 08:45:37'),(2,3,'king.richard@example.com',1,1,'2019-10-04 13:27:30','2019-10-04 13:27:30'),(3,4,'princejohn@example.com',1,1,'2019-10-04 13:27:30','2019-10-04 13:27:30'),(4,10,'manager1@example.net',1,1,'2009-07-22 06:32:07','2009-07-23 08:45:37'),(5,11,'manager2@example.net',1,1,'2009-07-22 06:32:07','2009-07-23 08:45:37'),(6,20,'user1@example.net',1,1,'2009-07-23 08:40:51','2015-10-16 09:08:39'),(7,21,'user2@example.net',1,1,'2009-07-23 08:40:51','2015-10-16 09:08:39'),(8,22,'UPPER@example.net',1,1,'2015-10-16 09:08:39','2015-10-16 09:08:39'),(9,30,'modify@example.net',1,1,'2009-07-23 08:40:51','2015-10-16 09:08:39'),(10,170,'Test@example.net',1,1,'2010-09-09 03:29:15','2012-08-30 09:49:02'),(11,234,'friar_tuck@example.org',1,1,'2019-10-04 13:27:30','2019-10-04 13:27:30'),(12,235,'noone@example.com',1,1,'2019-10-04 13:27:30','2019-10-04 13:27:30'),(13,236,'nobody@example.org',1,1,'2019-10-04 13:27:30','2019-10-04 13:27:30'),(14,1094,'robin_hood@example.org',0,1,'2019-10-04 13:27:30','2019-10-04 13:27:30'),(16,1947,'ws1@example.org',1,1,'2019-10-04 13:27:30','2019-10-04 13:27:30'),(17,4159,'alan_a_dale@example.org',1,1,'2019-10-04 13:27:30','2019-10-04 13:27:30'),(18,5,'robin_munn@sil.org',1,1,'2020-08-14 12:34:56','2020-08-14 12:34:56');
DROP TABLE IF EXISTS "enabled_modules";
CREATE TABLE "enabled_modules" (
    "id" integer NOT NULL,
    "project_id" integer DEFAULT NULL,
    "name" varchar(510) NOT NULL,
    PRIMARY KEY ("id")
);

DROP TABLE IF EXISTS "enumerations";
CREATE TABLE "enumerations" (
    "id" integer NOT NULL,
    "name" varchar(60) NOT NULL DEFAULT '',
    "position" integer DEFAULT NULL,
    "is_default" boolean NOT NULL DEFAULT false,
    "type" varchar(510) DEFAULT NULL,
    "active" boolean NOT NULL DEFAULT true,
    "project_id" integer DEFAULT NULL,
    "parent_id" integer DEFAULT NULL,
    "position_name" varchar(60) DEFAULT NULL,
    PRIMARY KEY ("id")
);

DROP TABLE IF EXISTS "groups_users";
CREATE TABLE "groups_users" (
    "group_id" integer NOT NULL,
    "user_id" integer NOT NULL,
    UNIQUE ("group_id","user_id")
);

DROP TABLE IF EXISTS "import_items";
CREATE TABLE "import_items" (
    "id" integer NOT NULL,
    "import_id" integer NOT NULL,
    "position" integer NOT NULL,
    "obj_id" integer DEFAULT NULL,
    "message" text ,
    PRIMARY KEY ("id")
);

DROP TABLE IF EXISTS "imports";
CREATE TABLE "imports" (
    "id" integer NOT NULL,
    "type" varchar(510) DEFAULT NULL,
    "user_id" integer NOT NULL,
    "filename" varchar(510) DEFAULT NULL,
    "settings" text ,
    "total_items" integer DEFAULT NULL,
    "finished" boolean NOT NULL DEFAULT false,
    "created_at" timestamp with time zone NOT NULL,
    "updated_at" timestamp with time zone NOT NULL,
    PRIMARY KEY ("id")
);

DROP TABLE IF EXISTS "issue_categories";
CREATE TABLE "issue_categories" (
    "id" integer NOT NULL,
    "project_id" integer NOT NULL DEFAULT '0',
    "name" varchar(120) NOT NULL DEFAULT '',
    "assigned_to_id" integer DEFAULT NULL,
    PRIMARY KEY ("id")
);

DROP TABLE IF EXISTS "issue_relations";
CREATE TABLE "issue_relations" (
    "id" integer NOT NULL,
    "issue_from_id" integer NOT NULL,
    "issue_to_id" integer NOT NULL,
    "relation_type" varchar(510) NOT NULL DEFAULT '',
    "delay" integer DEFAULT NULL,
    PRIMARY KEY ("id"),
    UNIQUE ("issue_from_id","issue_to_id")
);

DROP TABLE IF EXISTS "issue_statuses";
CREATE TABLE "issue_statuses" (
    "id" integer NOT NULL,
    "name" varchar(60) NOT NULL DEFAULT '',
    "is_closed" boolean NOT NULL DEFAULT false,
    "position" integer DEFAULT NULL,
    "default_done_ratio" integer DEFAULT NULL,
    PRIMARY KEY ("id")
);

DROP TABLE IF EXISTS "issues";
CREATE TABLE "issues" (
    "id" integer NOT NULL,
    "tracker_id" integer NOT NULL,
    "project_id" integer NOT NULL,
    "subject" varchar(510) NOT NULL DEFAULT '',
    "description" text ,
    "due_date" date DEFAULT NULL,
    "category_id" integer DEFAULT NULL,
    "status_id" integer NOT NULL,
    "assigned_to_id" integer DEFAULT NULL,
    "priority_id" integer NOT NULL,
    "fixed_version_id" integer DEFAULT NULL,
    "author_id" integer NOT NULL,
    "lock_version" integer NOT NULL DEFAULT '0',
    "created_on" timestamp with time zone DEFAULT NULL,
    "updated_on" timestamp with time zone DEFAULT NULL,
    "start_date" date DEFAULT NULL,
    "done_ratio" integer NOT NULL DEFAULT '0',
    "estimated_hours" float DEFAULT NULL,
    "parent_id" integer DEFAULT NULL,
    "root_id" integer DEFAULT NULL,
    "lft" integer DEFAULT NULL,
    "rgt" integer DEFAULT NULL,
    "is_private" boolean NOT NULL DEFAULT false,
    "closed_on" timestamp with time zone DEFAULT NULL,
    PRIMARY KEY ("id")
);

DROP TABLE IF EXISTS "journal_details";
CREATE TABLE "journal_details" (
    "id" integer NOT NULL,
    "journal_id" integer NOT NULL DEFAULT '0',
    "property" varchar(60) NOT NULL DEFAULT '',
    "prop_key" varchar(60) NOT NULL DEFAULT '',
    "old_value" text ,
    "value" text ,
    PRIMARY KEY ("id")
);

DROP TABLE IF EXISTS "journals";
CREATE TABLE "journals" (
    "id" integer NOT NULL,
    "journalized_id" integer NOT NULL DEFAULT '0',
    "journalized_type" varchar(60) NOT NULL DEFAULT '',
    "user_id" integer NOT NULL DEFAULT '0',
    "notes" text ,
    "created_on" timestamp with time zone NOT NULL,
    "private_notes" boolean NOT NULL DEFAULT false,
    PRIMARY KEY ("id")
);

DROP TABLE IF EXISTS "member_roles";
CREATE TABLE "member_roles" (
    "id" integer NOT NULL,
    "member_id" integer NOT NULL,
    "role_id" integer NOT NULL,
    "inherited_from" integer DEFAULT NULL,
    PRIMARY KEY ("id")
);

INSERT INTO "member_roles" VALUES (1,2,3,NULL),(2,3,4,NULL),(3,4,4,NULL),(4,5,3,NULL),(5,6,4,NULL),(6,7,3,NULL),(7,8,6,NULL),(8,8,3,NULL),(9,9,3,NULL),(45,69,3,NULL),(46,70,4,NULL),(352,500,3,NULL),(361,509,5,NULL),(3715,4822,4,NULL),(5606,7115,3,NULL),(6605,8250,3,NULL),(6607,8251,3,6605),(6608,8252,3,6605),(6614,8259,3,NULL),(6617,8262,3,NULL),(6618,8263,3,NULL),(7047,8692,3,NULL),(7102,8747,6,NULL),(7162,8807,4,NULL),(7795,9440,3,NULL);
DROP TABLE IF EXISTS "members";
CREATE TABLE "members" (
    "id" integer NOT NULL,
    "user_id" integer NOT NULL DEFAULT '0',
    "project_id" integer NOT NULL DEFAULT '0',
    "created_on" timestamp with time zone DEFAULT NULL,
    "mail_notification" boolean NOT NULL DEFAULT false,
    PRIMARY KEY ("id"),
    UNIQUE ("user_id","project_id")
);

INSERT INTO "members" VALUES (2,10,2,'2009-07-27 02:03:33',0),(3,20,2,'2009-07-27 02:03:33',0),(4,170,2,'2017-01-02 03:04:55',0),(5,11,3,'2009-07-27 02:03:33',0),(6,21,3,'2009-07-27 02:03:33',0),(7,170,3,'2017-01-02 03:04:55',0),(8,170,4,'2017-02-02 04:04:55',0),(9,170,7,'2017-02-02 04:04:55',0),(69,3,9,'2009-10-12 03:42:10',0),(70,20,9,'2009-10-12 03:42:19',0),(500,234,9,'2011-09-13 06:26:57',0),(509,256,9,'2011-10-12 06:03:49',0),(4822,234,1289,'2016-08-29 09:55:07',0),(7115,1947,1894,'2018-07-23 10:11:19',0),(8250,1094,2145,'2019-10-08 04:06:52',0),(8251,1094,2146,'2019-10-08 04:09:24',0),(8252,1094,2147,'2019-10-09 05:21:30',0),(8259,4159,2150,'2019-10-18 14:00:00',0),(8262,4159,2152,'2019-10-22 20:36:13',0),(8263,234,2153,'2019-10-24 07:09:46',0),(8692,1094,2255,'2021-02-18 10:50:32',0),(8747,5,1289,'2021-02-18 17:12:08',0),(8807,1094,1289,'2021-02-23 11:33:58',0),(9440,1094,9,'2021-04-19 14:24:47',0);
DROP TABLE IF EXISTS "messages";
CREATE TABLE "messages" (
    "id" integer NOT NULL,
    "board_id" integer NOT NULL,
    "parent_id" integer DEFAULT NULL,
    "subject" varchar(510) NOT NULL DEFAULT '',
    "content" text ,
    "author_id" integer DEFAULT NULL,
    "replies_count" integer NOT NULL DEFAULT '0',
    "last_reply_id" integer DEFAULT NULL,
    "created_on" timestamp with time zone NOT NULL,
    "updated_on" timestamp with time zone NOT NULL,
    "locked" int4 DEFAULT '0',
    "sticky" integer DEFAULT '0',
    PRIMARY KEY ("id")
);

DROP TABLE IF EXISTS "news";
CREATE TABLE "news" (
    "id" integer NOT NULL,
    "project_id" integer DEFAULT NULL,
    "title" varchar(120) NOT NULL DEFAULT '',
    "summary" varchar(510) DEFAULT '',
    "description" text ,
    "author_id" integer NOT NULL DEFAULT '0',
    "created_on" timestamp with time zone DEFAULT NULL,
    "comments_count" integer NOT NULL DEFAULT '0',
    PRIMARY KEY ("id")
);

DROP TABLE IF EXISTS "open_id_authentication_associations";
CREATE TABLE "open_id_authentication_associations" (
    "id" integer NOT NULL,
    "issued" integer DEFAULT NULL,
    "lifetime" integer DEFAULT NULL,
    "handle" varchar(510) DEFAULT NULL,
    "assoc_type" varchar(510) DEFAULT NULL,
    "server_url" bytea ,
    "secret" bytea ,
    PRIMARY KEY ("id")
);

DROP TABLE IF EXISTS "open_id_authentication_nonces";
CREATE TABLE "open_id_authentication_nonces" (
    "id" integer NOT NULL,
    "timestamp" integer NOT NULL,
    "server_url" varchar(510) DEFAULT NULL,
    "salt" varchar(510) NOT NULL,
    PRIMARY KEY ("id")
);

DROP TABLE IF EXISTS "phantom2";
DROP TABLE IF EXISTS "phantom1";
CREATE TABLE "phantom1" (
    "id" int4 NOT NULL,
    PRIMARY KEY ("id")
);

CREATE TABLE "phantom2" (
    "id" int4 NOT NULL,
    PRIMARY KEY ("id")
);

DROP TABLE IF EXISTS "plugin_schema_info";
CREATE TABLE "plugin_schema_info" (
    "plugin_name" varchar(510) DEFAULT NULL,
    "version" integer DEFAULT NULL
);

DROP TABLE IF EXISTS "projects";
CREATE TABLE "projects" (
    "id" integer NOT NULL,
    "name" varchar(510) NOT NULL DEFAULT '',
    "description" text ,
    "homepage" varchar(510) DEFAULT '',
    "is_public" boolean NOT NULL DEFAULT true,
    "parent_id" integer DEFAULT NULL,
    "created_on" timestamp with time zone DEFAULT NULL,
    "updated_on" timestamp with time zone DEFAULT NULL,
    "identifier" varchar(510) DEFAULT NULL,
    "status" integer NOT NULL DEFAULT '1',
    "lft" integer DEFAULT NULL,
    "rgt" integer DEFAULT NULL,
    "inherit_members" boolean NOT NULL DEFAULT false,
    "default_version_id" integer DEFAULT NULL,
    "default_assigned_to_id" integer DEFAULT NULL,
    PRIMARY KEY ("id")
);

INSERT INTO "projects" VALUES (1,'LD Test','LD API Test project','',0,NULL,'2009-07-23 09:56:52','2017-02-24 09:56:52','ld-test',1,NULL,NULL,0,NULL,NULL),(2,'LD Test Dictionary','LD API Test Dictionary project','',1,NULL,'2011-07-24 05:24:19','2017-02-24 02:33:33','test-ld-dictionary',1,3,4,0,NULL,NULL),(3,'LD API Test Flex','LD API Test FLEx project','',1,NULL,'2012-09-21 02:44:47','2017-02-24 02:44:47','test-ld-flex',1,5,6,0,NULL,NULL),(4,'LD API Test Demo','LD API Test Demo project','',1,NULL,'2013-09-21 02:44:47','2017-02-24 02:44:47','test-ld-demo',1,7,8,0,NULL,NULL),(5,'LD API Test AdaptIT','LD API Test AdaptIT project','',1,NULL,'2014-09-21 02:44:47','2017-02-24 02:44:47','test-ld-adapt',1,9,10,0,NULL,NULL),(6,'LD API Test Training','LD API Test Training project','',1,NULL,'2015-09-21 02:44:47','2017-02-24 02:44:47','test-ld-training',1,11,12,0,NULL,NULL),(7,'LD API UTF8 E�coding','LD API Test UTF8 E�coding project','',1,NULL,'2016-08-10 07:30:45','2017-03-01 08:10:20','test-ld-�tf8',1,13,14,0,NULL,NULL),(9,'Thai Food Dictionary','A picture dictionary of Thai food.','',1,NULL,'2009-10-12 03:41:53','2021-04-19 14:24:47','tha-food',1,17,18,0,NULL,NULL),(1289,'Sherwood TestSena3 03','','',1,NULL,'2016-08-25 07:58:11','2021-02-23 11:33:58','test-sherwood-sena-03',1,2379,2380,0,NULL,NULL),(1894,'test-ws-1-flex','','',1,NULL,'2018-07-23 09:31:24','2019-10-04 13:22:26','test-ws-1-flex',1,3513,3514,0,NULL,NULL),(2145,'Robin Test Projects','Test projects for Robin Hood testing Send/Receive scenarios','',1,NULL,'2019-10-08 04:06:32','2019-10-08 04:06:32','robin-test-projects',1,3999,4022,0,NULL,NULL),(2146,'Robin Test FLEx new public','','',1,2145,'2019-10-08 04:09:24','2019-10-08 04:09:24','test-robin-flex-new-public',1,4008,4009,1,NULL,NULL),(2147,'Robin new public 2','','',1,2145,'2019-10-09 05:21:30','2019-10-09 05:21:30','test-robin-new-public-2',1,4000,4001,1,NULL,NULL),(2150,'Alan_test','To test hg pull/push','',1,NULL,'2019-10-18 13:59:45','2019-10-22 20:13:53','alan_test',5,3997,3998,0,NULL,NULL),(2152,'aland_test','hg pull/push tests','',1,NULL,'2019-10-22 20:34:59','2019-10-22 20:34:59','aland_test',1,3995,3996,0,NULL,NULL),(2153,'tha-food2','','',1,NULL,'2019-10-24 07:06:20','2019-10-24 07:06:20','tha-food2',1,4023,4024,0,NULL,NULL),(2255,'New project via POST','testing POST','',1,NULL,'2021-02-18 10:50:31','2021-02-18 11:03:36','new-project-via-post',1,NULL,NULL,0,NULL,NULL);
DROP TABLE IF EXISTS "projects_trackers";
CREATE TABLE "projects_trackers" (
    "project_id" integer NOT NULL DEFAULT '0',
    "tracker_id" integer NOT NULL DEFAULT '0',
    UNIQUE ("project_id","tracker_id")
);

DROP TABLE IF EXISTS "queries";
CREATE TABLE "queries" (
    "id" integer NOT NULL,
    "project_id" integer DEFAULT NULL,
    "name" varchar(510) NOT NULL DEFAULT '',
    "filters" text ,
    "user_id" integer NOT NULL DEFAULT '0',
    "column_names" text ,
    "sort_criteria" text ,
    "group_by" varchar(510) DEFAULT NULL,
    "type" varchar(510) DEFAULT NULL,
    "visibility" integer DEFAULT '0',
    "options" text ,
    PRIMARY KEY ("id")
);

DROP TABLE IF EXISTS "queries_roles";
CREATE TABLE "queries_roles" (
    "query_id" integer NOT NULL,
    "role_id" integer NOT NULL,
    UNIQUE ("query_id","role_id")
);

DROP TABLE IF EXISTS "repositories";
CREATE TABLE "repositories" (
    "id" integer NOT NULL,
    "project_id" integer NOT NULL DEFAULT '0',
    "url" varchar(510) NOT NULL DEFAULT '',
    "login" varchar(120) DEFAULT '',
    "password" varchar(510) DEFAULT '',
    "root_url" varchar(510) DEFAULT '',
    "type" varchar(510) DEFAULT NULL,
    "path_encoding" varchar(128) DEFAULT NULL,
    "log_encoding" varchar(128) DEFAULT NULL,
    "extra_info" text ,
    "identifier" varchar(510) DEFAULT NULL,
    "is_default" int4 DEFAULT '0',
    "created_on" timestamp with time zone DEFAULT NULL,
    PRIMARY KEY ("id")
);

DROP TABLE IF EXISTS "roles";
CREATE TABLE "roles" (
    "id" integer NOT NULL,
    "name" varchar(60) NOT NULL DEFAULT '',
    "position" integer DEFAULT NULL,
    "assignable" int4 DEFAULT '1',
    "builtin" integer NOT NULL DEFAULT '0',
    "permissions" text ,
    "issues_visibility" varchar(60) NOT NULL DEFAULT 'default',
    "users_visibility" varchar(60) NOT NULL DEFAULT 'all',
    "time_entries_visibility" varchar(60) NOT NULL DEFAULT 'all',
    "all_roles_managed" boolean NOT NULL DEFAULT true,
    "settings" text ,
    PRIMARY KEY ("id")
);

INSERT INTO "roles" VALUES (1,'Non member',1,1,1,'--- \n- :add_messages\n- :view_documents\n- :view_files\n- :add_issues\n- :add_issue_notes\n- :save_queries\n- :view_gantt\n- :view_calendar\n- :comment_news\n- :view_time_entries\n- :view_wiki_pages\n- :view_wiki_edits\n- :view_issues\n- :view_news\n- :view_messages\n','default','all','all',1,NULL),(2,'Anonymous',2,1,2,'--- \n- :view_documents\n- :view_files\n- :view_gantt\n- :view_calendar\n- :view_time_entries\n- :view_wiki_pages\n- :view_wiki_edits\n- :view_issues\n- :view_news\n- :view_messages\n','default','all','all',1,NULL),(3,'Manager',3,1,0,'--- \n- :edit_project\n- :select_project_modules\n- :manage_members\n- :manage_versions\n- :manage_boards\n- :add_messages\n- :edit_messages\n- :edit_own_messages\n- :delete_messages\n- :delete_own_messages\n- :view_documents\n- :manage_files\n- :view_files\n- :manage_categories\n- :add_issues\n- :edit_issues\n- :manage_issue_relations\n- :add_issue_notes\n- :edit_issue_notes\n- :edit_own_issue_notes\n- :move_issues\n- :delete_issues\n- :manage_public_queries\n- :save_queries\n- :view_gantt\n- :view_calendar\n- :view_issue_watchers\n- :add_issue_watchers\n- :manage_news\n- :comment_news\n- :manage_repository\n- :browse_repository\n- :view_changesets\n- :commit_access\n- :log_time\n- :view_time_entries\n- :edit_time_entries\n- :edit_own_time_entries\n- :rename_wiki_pages\n- :delete_wiki_pages\n- :view_wiki_pages\n- :view_wiki_edits\n- :edit_wiki_pages\n- :delete_wiki_pages_attachments\n- :protect_wiki_pages\n- :view_issues\n- :add_documents\n- :edit_documents\n- :delete_documents\n- :view_news\n- :view_messages\n','default','all','all',1,NULL),(4,'Contributor',4,1,0,'--- \n- :manage_versions\n- :add_messages\n- :edit_own_messages\n- :view_documents\n- :manage_files\n- :view_files\n- :manage_categories\n- :add_issues\n- :edit_issues\n- :manage_issue_relations\n- :add_issue_notes\n- :edit_own_issue_notes\n- :save_queries\n- :view_gantt\n- :view_calendar\n- :view_issue_watchers\n- :manage_news\n- :comment_news\n- :browse_repository\n- :view_changesets\n- :commit_access\n- :log_time\n- :view_time_entries\n- :rename_wiki_pages\n- :delete_wiki_pages\n- :view_wiki_pages\n- :view_wiki_edits\n- :edit_wiki_pages\n- :delete_wiki_pages_attachments\n- :protect_wiki_pages\n- :view_issues\n- :add_documents\n- :edit_documents\n- :delete_documents\n- :view_news\n- :view_messages\n','default','all','all',1,NULL),(5,'Obv - do not use',5,1,0,'--- \n- :add_messages\n- :edit_own_messages\n- :view_documents\n- :view_files\n- :add_issues\n- :add_issue_notes\n- :save_queries\n- :view_gantt\n- :view_calendar\n- :comment_news\n- :browse_repository\n- :view_changesets\n- :log_time\n- :view_time_entries\n- :view_wiki_pages\n- :view_wiki_edits\n- :view_issues\n- :view_news\n- :view_messages\n','default','all','all',1,NULL),(6,'LanguageDepotProgrammer',6,1,0,'--- \n- :add_messages\n- :view_documents\n- :view_files\n- :add_issues\n- :add_issue_notes\n- :save_queries\n- :view_gantt\n- :view_calendar\n- :comment_news\n- :browse_repository\n- :view_changesets\n- :view_time_entries\n- :view_issues\n- :view_news\n- :view_messages\n','default','all','all',1,NULL);
DROP TABLE IF EXISTS "roles_managed_roles";
CREATE TABLE "roles_managed_roles" (
    "role_id" integer NOT NULL,
    "managed_role_id" integer NOT NULL,
    UNIQUE ("role_id","managed_role_id")
);

DROP TABLE IF EXISTS "schema_migrations";
CREATE TABLE "schema_migrations" (
    "version" varchar(510) NOT NULL,
    UNIQUE ("version")
);

DROP TABLE IF EXISTS "settings";
CREATE TABLE "settings" (
    "id" integer NOT NULL,
    "name" varchar(510) NOT NULL DEFAULT '',
    "value" text ,
    "updated_on" timestamp with time zone DEFAULT NULL,
    PRIMARY KEY ("id")
);

DROP TABLE IF EXISTS "time_entries";
CREATE TABLE "time_entries" (
    "id" integer NOT NULL,
    "project_id" integer NOT NULL,
    "user_id" integer NOT NULL,
    "issue_id" integer DEFAULT NULL,
    "hours" float NOT NULL,
    "comments" varchar(2048) DEFAULT NULL,
    "activity_id" integer NOT NULL,
    "spent_on" date NOT NULL,
    "tyear" integer NOT NULL,
    "tmonth" integer NOT NULL,
    "tweek" integer NOT NULL,
    "created_on" timestamp with time zone NOT NULL,
    "updated_on" timestamp with time zone NOT NULL,
    PRIMARY KEY ("id")
);

DROP TABLE IF EXISTS "tokens";
CREATE TABLE "tokens" (
    "id" integer NOT NULL,
    "user_id" integer NOT NULL DEFAULT '0',
    "action" varchar(60) NOT NULL DEFAULT '',
    "value" varchar(80) NOT NULL DEFAULT '',
    "created_on" timestamp with time zone NOT NULL,
    "updated_on" timestamp NULL DEFAULT NULL,
    PRIMARY KEY ("id"),
    UNIQUE ("value")
);

DROP TABLE IF EXISTS "trackers";
CREATE TABLE "trackers" (
    "id" integer NOT NULL,
    "name" varchar(60) NOT NULL DEFAULT '',
    "is_in_chlog" boolean NOT NULL DEFAULT false,
    "position" integer DEFAULT NULL,
    "is_in_roadmap" boolean NOT NULL DEFAULT true,
    "fields_bits" integer DEFAULT '0',
    "default_status_id" integer DEFAULT NULL,
    PRIMARY KEY ("id")
);

INSERT INTO "trackers" VALUES (1,'Bug',1,1,0,0,1),(2,'Feature',1,2,1,0,1),(3,'Support',0,3,0,0,1);
DROP TABLE IF EXISTS "user_preferences";
CREATE TABLE "user_preferences" (
    "id" integer NOT NULL,
    "user_id" integer NOT NULL DEFAULT '0',
    "others" text ,
    "hide_mail" int4 DEFAULT '1',
    "time_zone" varchar(510) DEFAULT NULL,
    PRIMARY KEY ("id")
);

DROP TABLE IF EXISTS "users";
CREATE TABLE "users" (
    "id" integer NOT NULL,
    "login" varchar(510) NOT NULL DEFAULT '',
    "hashed_password" varchar(80) NOT NULL DEFAULT '',
    "firstname" varchar(60) NOT NULL DEFAULT '',
    "lastname" varchar(510) NOT NULL DEFAULT '',
    "admin" boolean NOT NULL DEFAULT false,
    "status" integer NOT NULL DEFAULT '1',
    "last_login_on" timestamp with time zone DEFAULT NULL,
    "language" varchar(10) DEFAULT '',
    "auth_source_id" integer DEFAULT NULL,
    "created_on" timestamp with time zone DEFAULT NULL,
    "updated_on" timestamp with time zone DEFAULT NULL,
    "type" varchar(510) DEFAULT NULL,
    "identity_url" varchar(510) DEFAULT NULL,
    "mail_notification" varchar(510) NOT NULL DEFAULT '',
    "salt" varchar(128) DEFAULT NULL,
    "must_change_passwd" boolean NOT NULL DEFAULT false,
    "passwd_changed_on" timestamp with time zone DEFAULT NULL,
    PRIMARY KEY ("id")
);

INSERT INTO "users" VALUES (1,'admin','7eeae5aa145d3ab61ff0e80d07ba02d573537a35','Admin','User',1,1,'2009-07-23 08:44:48','en',NULL,'2009-07-22 06:32:07','2009-07-23 08:45:37','User',NULL,'all','c5acc2da57548ddb2f1a228fab5c0071',0,'2019-10-15 07:45:50'),(2,'','','','Anonymous',0,0,NULL,'',NULL,'2009-07-22 08:44:34','2009-07-22 08:44:34','AnonymousUser',NULL,'only_my_events',NULL,0,NULL),(3,'richard','','Richard','Lionheart',1,1,'2019-05-31 07:05:05','en',NULL,'2009-07-23 08:40:51','2019-05-31 07:05:05','User',NULL,'only_my_events','3fd636ad724f378e648c343def141bcb',0,NULL),(4,'prince_john','','Prince','John',1,1,'2018-05-30 19:39:34','en',NULL,'2009-07-23 10:40:09','2018-05-30 19:39:34','User',NULL,'all','aa9ba054485f376979dfd561cd69dbf8',0,NULL),(5,'rmunn','','Robin','Munn',1,1,'2020-08-10 12:34:56','en',NULL,'2020-08-10 12:34:56','2020-08-10 12:34:56','User',NULL,'only_my_events','c5acc2da57548ddb2f1a228fab5c0071',0,NULL),(10,'manager1','bc852d2e71e76cf734e3a4b74619bc28d867c8bd','Manager1','User',0,1,'2009-07-23 08:44:48','en',NULL,'2009-07-22 06:32:07','2009-07-23 08:45:37','User',NULL,'only_my_events','dd903a045f4537436a257ce31b0c680c',0,NULL),(11,'manager2','5857a28060d630a5ed9e0bfd4e6e17a76fa41b79','Manager2','User',0,1,'2009-07-23 08:44:48','en',NULL,'2009-07-22 06:32:07','2009-07-23 08:45:37','User',NULL,'only_my_events','dd903a045f4537436a257ce31b0c680c',0,NULL),(20,'user1','02484720fe235a6fa352ffa0d5dac80897008ec0','User','One',0,1,'2015-10-16 09:08:39','en',NULL,'2009-07-23 08:40:51','2015-10-16 09:08:39','User',NULL,'only_my_events','dd903a045f4537436a257ce31b0c680c',0,NULL),(21,'user2','3dd4ba95e5e68cd43d430a1a2d74a9ce75957be9','User','Two',0,1,'2015-10-16 09:08:39','en',NULL,'2009-07-23 08:40:51','2015-10-16 09:08:39','User',NULL,'only_my_events','dd903a045f4537436a257ce31b0c680c',0,NULL),(22,'Upper','721c93a8a9238620123d3bcfa670ce56','Upper','Case',0,1,'2015-10-21 09:08:39','en',NULL,'2015-10-16 09:08:39','2015-10-16 09:08:39','User',NULL,'only_my_events','dd903a045f4537436a257ce31b0c680c',0,NULL),(30,'modify','9f37b795e5468cdf3e4a0a4a2d54698e056556e7','Modify','User',0,1,'2015-10-16 09:08:39','en',NULL,'2009-07-23 08:40:51','2015-10-16 09:08:39','User',NULL,'only_my_events','dd903a045f4537436a257ce31b0c680c',0,NULL),(170,'test','d8bebbafb32fbb0545773ce30dbcfb29e7573050','Test','Palaso',0,1,'2015-10-16 09:08:39','en',NULL,'2010-09-09 03:29:15','2012-08-30 09:49:02','User',NULL,'only_my_events','dd903a045f4537436a257ce31b0c680c',0,NULL),(234,'tuck','08099b4bf0670e72f2a1a364e417bcf9b8a8b681','Friar','Tuck',1,1,'2019-10-24 07:15:55','en',NULL,'2011-02-03 02:25:45','2019-08-23 04:38:34','User',NULL,'only_my_events','1713448be6bb43818d5067b3f3110052',0,NULL),(235,'guest','','Guest','Observer',0,1,'2013-02-27 08:43:53','en',NULL,'2011-02-03 06:48:14','2013-02-27 08:43:53','User',NULL,'only_my_events','0e2663561e75495bbe8f24d98c7b14af',0,NULL),(256,'guest-palaso','','Guest','Palaso',0,1,NULL,'en',NULL,'2011-03-10 08:20:05','2011-03-10 08:20:05','User',NULL,'only_my_events','75a51aa7977a4966ad775e90215d581e',0,NULL),(1094,'rhood','7eeae5aa145d3ab61ff0e80d07ba02d573537a35','Robin','Hood',1,1,'2019-10-30 08:07:45','en',NULL,'2014-02-04 03:59:06','2019-10-15 07:45:50','User',NULL,'only_my_events','c5acc2da57548ddb2f1a228fab5c0071',0,'2019-10-15 07:45:50'),(1947,'willscarlet','','Will','Scarlet',1,1,'2019-09-27 15:42:25','en',NULL,'2015-10-14 05:54:01','2019-09-27 15:42:25','User',NULL,'only_my_events','981f7ecfdb494e01a133ea5813cb4f3a',0,NULL),(4159,'adale','','Alan','a Dale',1,1,'2019-10-30 13:21:01','en',NULL,'2019-07-25 14:14:46','2019-10-03 22:05:18','User',NULL,'only_my_events','eebaa2330def4e51be7fe5587baf18d0',0,NULL);
DROP TABLE IF EXISTS "versions";
CREATE TABLE "versions" (
    "id" integer NOT NULL,
    "project_id" integer NOT NULL DEFAULT '0',
    "name" varchar(510) NOT NULL DEFAULT '',
    "description" varchar(510) DEFAULT '',
    "effective_date" date DEFAULT NULL,
    "created_on" timestamp with time zone DEFAULT NULL,
    "updated_on" timestamp with time zone DEFAULT NULL,
    "wiki_page_title" varchar(510) DEFAULT NULL,
    "status" varchar(510) DEFAULT 'open',
    "sharing" varchar(510) NOT NULL DEFAULT 'none',
    PRIMARY KEY ("id")
);

DROP TABLE IF EXISTS "watchers";
CREATE TABLE "watchers" (
    "id" integer NOT NULL,
    "watchable_type" varchar(510) NOT NULL DEFAULT '',
    "watchable_id" integer NOT NULL DEFAULT '0',
    "user_id" integer DEFAULT NULL,
    PRIMARY KEY ("id")
);

DROP TABLE IF EXISTS "wiki_content_versions";
CREATE TABLE "wiki_content_versions" (
    "id" integer NOT NULL,
    "wiki_content_id" integer NOT NULL,
    "page_id" integer NOT NULL,
    "author_id" integer DEFAULT NULL,
    "data" bytea ,
    "compression" varchar(12) DEFAULT '',
    "comments" varchar(2048) DEFAULT '',
    "updated_on" timestamp with time zone NOT NULL,
    "version" integer NOT NULL,
    PRIMARY KEY ("id")
);

DROP TABLE IF EXISTS "wiki_contents";
CREATE TABLE "wiki_contents" (
    "id" integer NOT NULL,
    "page_id" integer NOT NULL,
    "author_id" integer DEFAULT NULL,
    "text" text ,
    "comments" varchar(2048) DEFAULT '',
    "updated_on" timestamp with time zone NOT NULL,
    "version" integer NOT NULL,
    PRIMARY KEY ("id")
);

DROP TABLE IF EXISTS "wiki_pages";
CREATE TABLE "wiki_pages" (
    "id" integer NOT NULL,
    "wiki_id" integer NOT NULL,
    "title" varchar(510) NOT NULL,
    "created_on" timestamp with time zone NOT NULL,
    "protected" boolean NOT NULL DEFAULT false,
    "parent_id" integer DEFAULT NULL,
    PRIMARY KEY ("id")
);

DROP TABLE IF EXISTS "wiki_redirects";
CREATE TABLE "wiki_redirects" (
    "id" integer NOT NULL,
    "wiki_id" integer NOT NULL,
    "title" varchar(510) DEFAULT NULL,
    "redirects_to" varchar(510) DEFAULT NULL,
    "created_on" timestamp with time zone NOT NULL,
    "redirects_to_wiki_id" integer NOT NULL,
    PRIMARY KEY ("id")
);

DROP TABLE IF EXISTS "wikis";
CREATE TABLE "wikis" (
    "id" integer NOT NULL,
    "project_id" integer NOT NULL,
    "start_page" varchar(510) NOT NULL,
    "status" integer NOT NULL DEFAULT '1',
    PRIMARY KEY ("id")
);

DROP TABLE IF EXISTS "workflows";
CREATE TABLE "workflows" (
    "id" integer NOT NULL,
    "tracker_id" integer NOT NULL DEFAULT '0',
    "old_status_id" integer NOT NULL DEFAULT '0',
    "new_status_id" integer NOT NULL DEFAULT '0',
    "role_id" integer NOT NULL DEFAULT '0',
    "assignee" boolean NOT NULL DEFAULT false,
    "author" boolean NOT NULL DEFAULT false,
    "type" varchar(60) DEFAULT NULL,
    "field_name" varchar(60) DEFAULT NULL,
    "rule" varchar(60) DEFAULT NULL,
    PRIMARY KEY ("id")
);


-- Post-data save --
COMMIT;
START TRANSACTION;

-- Foreign keys --
ALTER TABLE "phantom2" ADD CONSTRAINT "id_fkey" FOREIGN KEY ("id") REFERENCES "phantom1" ("id") DEFERRABLE INITIALLY DEFERRED;
CREATE INDEX ON "phantom2" ("id");

-- Sequences --
CREATE SEQUENCE attachments_id_seq;
SELECT setval('attachments_id_seq', max(id)) FROM attachments;
ALTER TABLE "attachments" ALTER COLUMN "id" SET DEFAULT nextval('attachments_id_seq');
CREATE SEQUENCE auth_sources_id_seq;
SELECT setval('auth_sources_id_seq', max(id)) FROM auth_sources;
ALTER TABLE "auth_sources" ALTER COLUMN "id" SET DEFAULT nextval('auth_sources_id_seq');
CREATE SEQUENCE boards_id_seq;
SELECT setval('boards_id_seq', max(id)) FROM boards;
ALTER TABLE "boards" ALTER COLUMN "id" SET DEFAULT nextval('boards_id_seq');
CREATE SEQUENCE changes_id_seq;
SELECT setval('changes_id_seq', max(id)) FROM changes;
ALTER TABLE "changes" ALTER COLUMN "id" SET DEFAULT nextval('changes_id_seq');
CREATE SEQUENCE changesets_id_seq;
SELECT setval('changesets_id_seq', max(id)) FROM changesets;
ALTER TABLE "changesets" ALTER COLUMN "id" SET DEFAULT nextval('changesets_id_seq');
CREATE SEQUENCE comments_id_seq;
SELECT setval('comments_id_seq', max(id)) FROM comments;
ALTER TABLE "comments" ALTER COLUMN "id" SET DEFAULT nextval('comments_id_seq');
CREATE SEQUENCE custom_field_enumerations_id_seq;
SELECT setval('custom_field_enumerations_id_seq', max(id)) FROM custom_field_enumerations;
ALTER TABLE "custom_field_enumerations" ALTER COLUMN "id" SET DEFAULT nextval('custom_field_enumerations_id_seq');
CREATE SEQUENCE custom_fields_id_seq;
SELECT setval('custom_fields_id_seq', max(id)) FROM custom_fields;
ALTER TABLE "custom_fields" ALTER COLUMN "id" SET DEFAULT nextval('custom_fields_id_seq');
CREATE SEQUENCE custom_values_id_seq;
SELECT setval('custom_values_id_seq', max(id)) FROM custom_values;
ALTER TABLE "custom_values" ALTER COLUMN "id" SET DEFAULT nextval('custom_values_id_seq');
CREATE SEQUENCE documents_id_seq;
SELECT setval('documents_id_seq', max(id)) FROM documents;
ALTER TABLE "documents" ALTER COLUMN "id" SET DEFAULT nextval('documents_id_seq');
CREATE SEQUENCE email_addresses_id_seq;
SELECT setval('email_addresses_id_seq', max(id)) FROM email_addresses;
ALTER TABLE "email_addresses" ALTER COLUMN "id" SET DEFAULT nextval('email_addresses_id_seq');
CREATE SEQUENCE enabled_modules_id_seq;
SELECT setval('enabled_modules_id_seq', max(id)) FROM enabled_modules;
ALTER TABLE "enabled_modules" ALTER COLUMN "id" SET DEFAULT nextval('enabled_modules_id_seq');
CREATE SEQUENCE enumerations_id_seq;
SELECT setval('enumerations_id_seq', max(id)) FROM enumerations;
ALTER TABLE "enumerations" ALTER COLUMN "id" SET DEFAULT nextval('enumerations_id_seq');
CREATE SEQUENCE import_items_id_seq;
SELECT setval('import_items_id_seq', max(id)) FROM import_items;
ALTER TABLE "import_items" ALTER COLUMN "id" SET DEFAULT nextval('import_items_id_seq');
CREATE SEQUENCE imports_id_seq;
SELECT setval('imports_id_seq', max(id)) FROM imports;
ALTER TABLE "imports" ALTER COLUMN "id" SET DEFAULT nextval('imports_id_seq');
CREATE SEQUENCE issue_categories_id_seq;
SELECT setval('issue_categories_id_seq', max(id)) FROM issue_categories;
ALTER TABLE "issue_categories" ALTER COLUMN "id" SET DEFAULT nextval('issue_categories_id_seq');
CREATE SEQUENCE issue_relations_id_seq;
SELECT setval('issue_relations_id_seq', max(id)) FROM issue_relations;
ALTER TABLE "issue_relations" ALTER COLUMN "id" SET DEFAULT nextval('issue_relations_id_seq');
CREATE SEQUENCE issue_statuses_id_seq;
SELECT setval('issue_statuses_id_seq', max(id)) FROM issue_statuses;
ALTER TABLE "issue_statuses" ALTER COLUMN "id" SET DEFAULT nextval('issue_statuses_id_seq');
CREATE SEQUENCE issues_id_seq;
SELECT setval('issues_id_seq', max(id)) FROM issues;
ALTER TABLE "issues" ALTER COLUMN "id" SET DEFAULT nextval('issues_id_seq');
CREATE SEQUENCE journal_details_id_seq;
SELECT setval('journal_details_id_seq', max(id)) FROM journal_details;
ALTER TABLE "journal_details" ALTER COLUMN "id" SET DEFAULT nextval('journal_details_id_seq');
CREATE SEQUENCE journals_id_seq;
SELECT setval('journals_id_seq', max(id)) FROM journals;
ALTER TABLE "journals" ALTER COLUMN "id" SET DEFAULT nextval('journals_id_seq');
CREATE SEQUENCE member_roles_id_seq;
SELECT setval('member_roles_id_seq', max(id)) FROM member_roles;
ALTER TABLE "member_roles" ALTER COLUMN "id" SET DEFAULT nextval('member_roles_id_seq');
CREATE SEQUENCE members_id_seq;
SELECT setval('members_id_seq', max(id)) FROM members;
ALTER TABLE "members" ALTER COLUMN "id" SET DEFAULT nextval('members_id_seq');
CREATE SEQUENCE messages_id_seq;
SELECT setval('messages_id_seq', max(id)) FROM messages;
ALTER TABLE "messages" ALTER COLUMN "id" SET DEFAULT nextval('messages_id_seq');
CREATE SEQUENCE news_id_seq;
SELECT setval('news_id_seq', max(id)) FROM news;
ALTER TABLE "news" ALTER COLUMN "id" SET DEFAULT nextval('news_id_seq');
CREATE SEQUENCE open_id_authentication_associations_id_seq;
SELECT setval('open_id_authentication_associations_id_seq', max(id)) FROM open_id_authentication_associations;
ALTER TABLE "open_id_authentication_associations" ALTER COLUMN "id" SET DEFAULT nextval('open_id_authentication_associations_id_seq');
CREATE SEQUENCE open_id_authentication_nonces_id_seq;
SELECT setval('open_id_authentication_nonces_id_seq', max(id)) FROM open_id_authentication_nonces;
ALTER TABLE "open_id_authentication_nonces" ALTER COLUMN "id" SET DEFAULT nextval('open_id_authentication_nonces_id_seq');
CREATE SEQUENCE phantom1_id_seq;
SELECT setval('phantom1_id_seq', max(id)) FROM phantom1;
ALTER TABLE "phantom1" ALTER COLUMN "id" SET DEFAULT nextval('phantom1_id_seq');
CREATE SEQUENCE phantom2_id_seq;
SELECT setval('phantom2_id_seq', max(id)) FROM phantom2;
ALTER TABLE "phantom2" ALTER COLUMN "id" SET DEFAULT nextval('phantom2_id_seq');
CREATE SEQUENCE projects_id_seq;
SELECT setval('projects_id_seq', max(id)) FROM projects;
ALTER TABLE "projects" ALTER COLUMN "id" SET DEFAULT nextval('projects_id_seq');
CREATE SEQUENCE queries_id_seq;
SELECT setval('queries_id_seq', max(id)) FROM queries;
ALTER TABLE "queries" ALTER COLUMN "id" SET DEFAULT nextval('queries_id_seq');
CREATE SEQUENCE repositories_id_seq;
SELECT setval('repositories_id_seq', max(id)) FROM repositories;
ALTER TABLE "repositories" ALTER COLUMN "id" SET DEFAULT nextval('repositories_id_seq');
CREATE SEQUENCE roles_id_seq;
SELECT setval('roles_id_seq', max(id)) FROM roles;
ALTER TABLE "roles" ALTER COLUMN "id" SET DEFAULT nextval('roles_id_seq');
CREATE SEQUENCE settings_id_seq;
SELECT setval('settings_id_seq', max(id)) FROM settings;
ALTER TABLE "settings" ALTER COLUMN "id" SET DEFAULT nextval('settings_id_seq');
CREATE SEQUENCE time_entries_id_seq;
SELECT setval('time_entries_id_seq', max(id)) FROM time_entries;
ALTER TABLE "time_entries" ALTER COLUMN "id" SET DEFAULT nextval('time_entries_id_seq');
CREATE SEQUENCE tokens_id_seq;
SELECT setval('tokens_id_seq', max(id)) FROM tokens;
ALTER TABLE "tokens" ALTER COLUMN "id" SET DEFAULT nextval('tokens_id_seq');
CREATE SEQUENCE trackers_id_seq;
SELECT setval('trackers_id_seq', max(id)) FROM trackers;
ALTER TABLE "trackers" ALTER COLUMN "id" SET DEFAULT nextval('trackers_id_seq');
CREATE SEQUENCE user_preferences_id_seq;
SELECT setval('user_preferences_id_seq', max(id)) FROM user_preferences;
ALTER TABLE "user_preferences" ALTER COLUMN "id" SET DEFAULT nextval('user_preferences_id_seq');
CREATE SEQUENCE users_id_seq;
SELECT setval('users_id_seq', max(id)) FROM users;
ALTER TABLE "users" ALTER COLUMN "id" SET DEFAULT nextval('users_id_seq');
CREATE SEQUENCE versions_id_seq;
SELECT setval('versions_id_seq', max(id)) FROM versions;
ALTER TABLE "versions" ALTER COLUMN "id" SET DEFAULT nextval('versions_id_seq');
CREATE SEQUENCE watchers_id_seq;
SELECT setval('watchers_id_seq', max(id)) FROM watchers;
ALTER TABLE "watchers" ALTER COLUMN "id" SET DEFAULT nextval('watchers_id_seq');
CREATE SEQUENCE wiki_content_versions_id_seq;
SELECT setval('wiki_content_versions_id_seq', max(id)) FROM wiki_content_versions;
ALTER TABLE "wiki_content_versions" ALTER COLUMN "id" SET DEFAULT nextval('wiki_content_versions_id_seq');
CREATE SEQUENCE wiki_contents_id_seq;
SELECT setval('wiki_contents_id_seq', max(id)) FROM wiki_contents;
ALTER TABLE "wiki_contents" ALTER COLUMN "id" SET DEFAULT nextval('wiki_contents_id_seq');
CREATE SEQUENCE wiki_pages_id_seq;
SELECT setval('wiki_pages_id_seq', max(id)) FROM wiki_pages;
ALTER TABLE "wiki_pages" ALTER COLUMN "id" SET DEFAULT nextval('wiki_pages_id_seq');
CREATE SEQUENCE wiki_redirects_id_seq;
SELECT setval('wiki_redirects_id_seq', max(id)) FROM wiki_redirects;
ALTER TABLE "wiki_redirects" ALTER COLUMN "id" SET DEFAULT nextval('wiki_redirects_id_seq');
CREATE SEQUENCE wikis_id_seq;
SELECT setval('wikis_id_seq', max(id)) FROM wikis;
ALTER TABLE "wikis" ALTER COLUMN "id" SET DEFAULT nextval('wikis_id_seq');
CREATE SEQUENCE workflows_id_seq;
SELECT setval('workflows_id_seq', max(id)) FROM workflows;
ALTER TABLE "workflows" ALTER COLUMN "id" SET DEFAULT nextval('workflows_id_seq');

-- Full Text keys --

COMMIT;
