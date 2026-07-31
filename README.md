# MOOC-Kubernetes-Submissions
Exercise repository for DevOps with Kubernetes 2026 \
CH 1: \
[1.1](https://github.com/jouni609/MOOC-Kubernetes-Submissions/tree/main/1.1/Log_Output) 
[1.2](https://github.com/jouni609/MOOC-Kubernetes-Submissions/tree/main/1.2/Server_Port) 
[1.3](https://github.com/jouni609/MOOC-Kubernetes-Submissions/blob/main/1.1/Log_Output/manifests/deployment.yaml) 
[1.4](https://github.com/jouni609/MOOC-Kubernetes-Submissions/blob/main/1.2/Server_Port/manifests/deployment.yaml) 
[1.5](https://github.com/jouni609/MOOC-Kubernetes-Submissions/tree/main/1.5/LandingPage)  
[1.6](https://github.com/jouni609/MOOC-Kubernetes-Submissions/tree/main/1.6/LandingPage) 
[1.7](https://github.com/jouni609/MOOC-Kubernetes-Submissions/tree/main/1.7/Log_Output) 
[1.8](https://github.com/jouni609/MOOC-Kubernetes-Submissions/tree/main/1.8/LandingPage) 
[1.9](https://github.com/jouni609/MOOC-Kubernetes-Submissions/tree/main/1.9/Ping_Pong) 
[1.10](https://github.com/jouni609/MOOC-Kubernetes-Submissions/tree/main/1.10/Log_Output) 
[1.11](https://github.com/jouni609/MOOC-Kubernetes-Submissions/tree/main/1.11) 
[1.12](https://github.com/jouni609/MOOC-Kubernetes-Submissions/tree/main/1.12) 
[1.13](https://github.com/jouni609/MOOC-Kubernetes-Submissions/tree/main/1.13)

CH 2: \
[2.1](https://github.com/jouni609/MOOC-Kubernetes-Submissions/tree/main/2.0)
[2.2](https://github.com/jouni609/MOOC-Kubernetes-Submissions/tree/main/2.2)
[2.3](https://github.com/jouni609/MOOC-Kubernetes-Submissions/blob/main/2.3/namespace.yaml)
[2.4](https://github.com/jouni609/MOOC-Kubernetes-Submissions/tree/main/2.4)
[2.5](https://github.com/jouni609/MOOC-Kubernetes-Submissions/tree/main/2.5/Log_Output)
[2.6](https://github.com/jouni609/MOOC-Kubernetes-Submissions/tree/main/2.6)
[2.7](https://github.com/jouni609/MOOC-Kubernetes-Submissions/tree/main/2.7)
[2.8](https://github.com/jouni609/MOOC-Kubernetes-Submissions/tree/main/2.8)
[2.9](https://github.com/jouni609/MOOC-Kubernetes-Submissions/tree/main/2.9)
[2.10](https://github.com/jouni609/MOOC-Kubernetes-Submissions/tree/main/2.10)

CH 3: \
[3.1](https://github.com/jouni609/MOOC-Kubernetes-Submissions/tree/main/3.1)
[3.2](https://github.com/jouni609/MOOC-Kubernetes-Submissions/tree/main/3.2)
[3.3](https://github.com/jouni609/MOOC-Kubernetes-Submissions/tree/main/3.3)
[3.4](https://github.com/jouni609/MOOC-Kubernetes-Submissions/tree/main/3.4)
[3.5](https://github.com/jouni609/MOOC-Kubernetes-Submissions/tree/main/3.5)
[3.6](https://github.com/jouni609/MOOC-Kubernetes-Submissions/tree/main/.github)
[3.7](https://github.com/jouni609/MOOC-Kubernetes-Submissions/tree/main/3.7)
[3.8](https://github.com/jouni609/MOOC-Kubernetes-Submissions/blob/main/.github/workflows/delete-environment.yaml)
[3.10](https://github.com/jouni609/MOOC-Kubernetes-Submissions/tree/main/3.10)
[3.11](https://github.com/jouni609/MOOC-Kubernetes-Submissions/tree/main/3.11)
[3.12](https://github.com/jouni609/MOOC-Kubernetes-Submissions/blob/main/3.12/Log_Output_Testrun.png)

CH 4: \
[4.1](https://github.com/jouni609/MOOC-Kubernetes-Submissions/tree/main/4.1)
[4.2](https://github.com/jouni609/MOOC-Kubernetes-Submissions/tree/main/4.2)
[4.3](https://github.com/jouni609/MOOC-Kubernetes-Submissions/tree/main/4.3)
[4.4](https://github.com/jouni609/MOOC-Kubernetes-Submissions/tree/main/4.4)
[4.5](https://github.com/jouni609/MOOC-Kubernetes-Submissions/tree/main/4.5)

------------------------------------------
Exercise **3.9**

DBaas vs DIY

First of all, the idea of DBaas vs DIY can be split into multiple categories:

**Cost**

In terms of costs DIY seems to be the more enticing option if the product you are developing already covers a PostgreSQL solution. In DIY solution you are mostly paying for the disk space (PVC), compute and the node replicas. My initial gut feeling is that DIY is the cheaper option.
DBaas in terms of costs consists of multiple different configurable variables and possible licencing fees. DBaaS has similar pricing and at a quick glance is impossible to tell which one would be the more economical choice. Your mileage may vary as it is up to you to optimize your cloud usage.

**Effort levels**

Effort levels consist of the required input to get the solution up and running in the cloud, alongside with the effort maintaining the solution when it's up in the cloud. GBaas seems to be the more efficient option here, if we eliminate the cost from this equation. Google Cloud offers a plethora of services alongside other service providers (AWS, Railway etc.). If you just want to deploy as fast as possible GBaas is the clear winner here.
DIY is clear loser in this race for maximum velocity, but the tradeoff is always with control of your software, and the flexibility to change platforms if for some reason a platform provider shoots up the prices. DIY solutions are generally more portable as it is your software and infrastructure, which you can uproot from the service provider.

**Portability**

As discussed in the effort level paragraph using platform providors own solutions generally tend to tie you and your software to a certain platform. DBaas solutions are generally quick to get up and running, and generally very usefull as long as you stay loyal to the platform and keep paying your bills. The downside here is that they make you think twice when changing service provider as it is quite the hassle to change.
DIY solutions usually have no issue with porting your software and business to another service provider. Personally I have experience in AWS, Google & Railway services and the user experience in railway is very slick in comparison to AWS & Google.

**Maintenance**

In DBaas services the service provider maintains the infrastructure, OS, security and so forth. Developer has to manage his own software, db settings, performance, config & connectivity. Developer does not have to waste his working hours on upkeeping the systems.
Using your own DIY solution means you have to do all the upkeep yourself, kubernetes does not maintain your pods, it can regenerate and restart failing pods but thats about it.

**Availability**

DBaas and DIY solutions can both cover availability across multiple regions to diminish the risk of low availability. The distinction here is that DBaaS services work out of the box if just configured. DIY solutions require a system level design on operating a replica + failover system.

**Scaling**

DBaas has a large range of different elastic services for storage, memory & load balancing traffic. DIY can cover all of these DBaas services, but again it requires bringing your own solutions, which in turn requires expertise. Rule of thumb here is that if you dont know what you are doing, just use DBaas services.

**Control**

DIY offers the most robust options here and is the clear #1 option if you need full control of your cluster. DBaas is clearly the more restricted option here, but in a sense of giving the developers and users guardrails to use the cloud platform.

**Backups**

Cloud platform providers contain built in systems for backups and have configurations for automated snapshots of your builds and so forth. Restoration process can be done through their own web interface or CLI (AWS Snapshot management was fairly easy to use through the web interface). These snapshots might lock you in to a certain ecosystem though developer should keep that in mind.
DIY solutions require to create your own solution, for example a CronJob to create a snapshot every now and then. PostgreSQL can be restored via these systems but require more expertise in the area. The larger plus comes from the portability aspect as your own solution, for example a pg_dump or a disk snapshot does not lock you in to a certain ecosystem.

Overall the choice between DBaas and DIY breaks down into time, money and expertise. If you eliminate one or two from this rubrick you get a clear winner, for example if you have infinite money, but no time and expertise DBaas is the most fitting option. DIY is the only option if you have no bankroll to use the large scale of DBaas solutions. Generally the solution lies inbetween these two, find services which benefit your software and business compare your spent on the DBaas.

------------------------------------------


