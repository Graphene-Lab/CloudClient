# Trustless cloud client

## The project is centered with a military safety standard.

# Easy to use Cloud Client + Backup Drive (Bonus)!

Cross-platform desktop cloud client, for compatible clouds (works in conjunction with cloud server to sync files).
This project has dependencies with other libraries which you can find open source in the same account as separate projects as the underlying libraries are in common with other projects:
Therefore, the projects not included can be added by searching for them on the Github repository, some are also available as Nuget packages (you can add them to the solution instead of the missing projects).
If you don't have the Cloud Server, alternatively you can use this software with a network address as a remote repository (the path must be set in the "git" parameter under the backup settings), for example you can set a pen drive connected to your router (in this case, the samba network path of the pen drive must be entered in "git").

# Description

To use this software you need to have the relevant Private Cloud and compatible Cloud service (otherwise where does the cloud connect?).
This program is an open source desktop cloud client to automatically synchronize, encrypted and with military grade security, files from your PC to your private cloud or cloud service.
The synchronization algorithms are very fast and the software with hundreds of thousands of files does not go into crisis as it happens with similar products.
This is an open source product and is published here in exactly the same source format version as you find here: https://github.com/Graphene-Lab/CloudClient without any additions or modifications.
Respect for your privacy is total, and the military level of security protects you and your data from hackers who would like to sneak into the cloud to access your personal data and information.

Safety:
We have transmitted our experience in the development of "non custodial wallet" bitcoins to this application so the underlying uses the same concepts and the same libraries, which are the foundations of the trustless technology used for the blockchain (the maximum current concept in terms of security) . The application generates a passphrase which creates a pair of cryptographic keys (public and private), which represent your digital identity and you can also sign documents with it. This digital identity is used by the server to recognize you, it is the underlying for encrypted communication. Using the passphrase you can restore your account on the client side, just like cryptocurrency wallets. Just like with bitcoin wallets, your account is client-side only, there is no website where you need to register or a place where your account is kept, which makes this application conceptually superior to all similar ones.

As a bonus we have added some extra features:
* Automatic virus detection (occurs at the same time as cloud file synchronization).
* Daily automatic backup: To take advantage of this function you need to have an additional physical HD since the backup done on the same disk is useless because it does not protect against physical disk failure. The backup uses hard link functions so it doesn't take up much space, new backups only take up the difference of what has changed since the previous backup was done.
* Versioning: A new backup is created every time a file is modified in order to keep its previous losses and allow rollback (very useful function for software developers).
* Synchronization of the cloud area on the pen drive or disk attached to the router: You can specify the network path of your device connected to the router's USB port and have a real-time synchronized copy of all your data so if your computer were stolen or should you lose it, you will still have a copy of all your data.
* Digital signature of documents, i.e. the software creates a digital identity with which you can sign documents and validate the signature affixed by others.

The software needs to run in administrator mode for the following reasons:
* Automatic date and time adjustment in your computer (if the date is wrong the files will be recorded with wrong dates and could be mistakenly mistaken as older than versions contained on the cloud).
* Create hard links for backups (this saves a lot of space during backups).

# Installation
If you want to customize the cloud folder path, enable or disable virtual disk and other settings, you need to edit the **appsettings.json** file and read the notes inside for more clarifications. The default configuration is fine for most users.
If you are a Linux and MacOS user, you can simply run install.sh to install the application:
```sh
chmod +x install.sh
./install.sh
```
If you use Windows we recommend unpacking the files in the "C:\Program Files\Cloud" folder. Launch the executable (Cloud.exe) to start the cloud and complete the installation (at the first start you may be asked to install runtime components, these are open source components that are required, if you do not want these components you need to compile the application without these dependencies)
It is possible to connect your PC with multiple clouds, in which case it is necessary to reinstall the application in a different path, modifying the appsettings.json file, so that the port for the web interface is different from those already used, and also setting a CloudPath that is different from the paths already used for other cloud client instances. These rules to follow also apply to those who use Mac and Linux.

NOTE: Administrator/root privileges are required to allow the application to synchronize the system clock, give full access to the files, and to allow the application to create hard links for backup functions. It is important to work with the synchronized clock, so that you have the correct date and time on the files and allow the cloud to accurately recognize new versions of the files.

# Privacy Policy

The application does not collect or send personal data, all communication is exclusively with your private cloud or cloud service.
Since the privacy policy is trustless (you don't have to trust us but it is the code that demonstrates honesty), the source code irrefutably demonstrates that our work is sincere and loyal and that your data is protected, and the communications between client and Cloud server are impossible to intercept because they are covered by military-grade encryption systems with digitally signed packets to prevent "man in the middle" attacks in a preventive manner. The versions that we publish on the store are the same (without any modifications) that you can find here in source format.


# The Illusion of Data Protection: How EU Regulations Enable Mass Surveillance and the Trustless Solution

In Europe, the General Data Protection Regulation (GDPR) was introduced with the noble intention of safeguarding citizens' privacy by imposing strict rules on the custody and processing of personal data. On the surface, these regulations appear robust, designed to prevent misuse and unauthorized profiling. Yet, beneath this façade of compliance lies a disturbing reality: these very rules have become a tool for systemic data exploitation. The GDPR, rather than being a shield against surveillance, has instead institutionalized a framework that facilitates the mass profiling of individuals—often under the guise of legality.  

The revelations brought to light by Edward Snowden and the subsequent *Datagate* scandal exposed a global surveillance apparatus where corporations and intelligence agencies collaborated in harvesting and analyzing personal data on an unprecedented scale. This was not mere speculation; the investigative journalism that uncovered these practices was awarded the Pulitzer Prize, cementing its credibility. The scandal revealed that personal data was not just being stored—it was being weaponized for purposes ranging from corporate espionage to political manipulation.  

What makes this even more alarming is that the very entities implicated in these breaches—Amazon, Google, Microsoft—now dominate Europe’s cloud storage market, operating in what amounts to a de facto monopoly. Logically, these corporations should have been barred from handling European citizens' data. Instead, they not only continue to operate but also provide cloud infrastructure to governments and national cybersecurity agencies. This paradox underscores a fundamental truth: the regulatory landscape is not shaped by genuine concern for privacy, but by corporate lobbying power. The result is a system where legislation, rather than protecting individuals, ensures that data flows seamlessly into the hands of those who seek to control and categorize entire populations.  

### The Fallacy of Certifications and the Rise of Trustless Security  

The cybersecurity industry has long relied on certifications and compliance standards as markers of trust. But this approach is fundamentally flawed. If a system requires external validation to prove its security, then it is inherently insecure—because it demands that users place blind faith in the certifying authority. The reality is that true security does not stem from bureaucratic approvals, but from mathematical certainty.  

This is where *trustless* technology revolutionizes data protection. Unlike traditional systems, which depend on centralized trust (and are therefore vulnerable to manipulation), trustless architectures ensure security through cryptographic algorithms that eliminate the need for intermediaries. The concept was pioneered by Bitcoin—a decentralized blockchain network that operates without certifications, where nodes exist in untrusted environments, yet the system remains unhackable because its security is intrinsic, not granted by external validators.  

Our data custody solution is built entirely on this principle. By leveraging *zero-knowledge encryption*, we ensure that no third party—whether a cloud provider, a government, or a malicious actor—can access or analyze user data. Here’s how it works:  

1. **Client-Side Encryption**: Before any data leaves a user’s device, it is encrypted using a 512-bit derived key, generated from a 12 or 24-word passphrase. This means the data is indecipherable the moment it is stored or transmitted.  
2. **No Server-Side Access**: Even if the data resides on a server, the custodian cannot read it. The encryption keys never leave the user’s control.  
3. **Secure Key Custody**: We provide hardware solutions, such as encrypted USB tokens, allowing users to physically possess their decryption keys. For example, a patient’s medical records can be stored in the cloud, but a doctor can only access them if the patient authorizes it by connecting their key.  

### A System Designed for a Hostile World  

The current regulatory environment is not an accident—it is a carefully constructed illusion, designed to maintain the dominance of surveillance-capable corporations. The GDPR’s loopholes and certifications serve as a smokescreen, enabling data harvesting under the pretense of compliance. Our solution bypasses this deception entirely by removing trust from the equation.  

Consider this: If a European hospital stores patient records on a "GDPR-compliant" cloud service operated by a Datagate-implicated corporation, those records are only as secure as the corporation’s willingness to follow the rules—rules that have already been proven unreliable. In contrast, with our trustless system, even if the data is stored on that same compromised cloud, it remains encrypted, unreadable, and entirely under the patient’s control.  

### Conclusion: Reclaiming Privacy in an Age of Surveillance  

The fight for true data privacy is not just a technical challenge—it is a battle against systemic exploitation. The existing regulatory framework, far from being a safeguard, is a facilitator of mass profiling. Trustless technology dismantles this machinery by ensuring that security is no longer a matter of policy, but of mathematics.  

We do not ask users to trust us—we ask them to trust encryption. Because in a world where corporations and governments have repeatedly betrayed public trust, the only real security is the kind that requires no faith at all.  

Our solution is not just an alternative. It is the *only* alternative.

## dependencies:

The libraries that are the engine of this app (and represent a dependency), must be downloaded from here:

* [Cloud Library](https://github.com/Graphene-Lab/CloudLibraries) are libraries for the creation of symmetric cloud (with server and equal client).

* [Encrypted Messenging](https://github.com/Graphene-Lab/EncryptedMessaging) are Low-level libraries for trustless encrypted socket connection (derived from bitcoin technology).

* [Secure Storage](https://github.com/Graphene-Lab/SecureStorage) It is a library used to save data and information produced by the application in an encrypted and inaccessible way.

* [AntiGit](https://github.com/Graphene-Lab/AntiGithub) , in this project you will find the libraries used for the data backup and redundancy functions.

NOTE: Any other (possible) dependencies are on [our GitHub](https://github.com/Graphene-Lab) in source format.

The reasons that led to this project with dontnet is that it is an open source development environment, and effective security is achieved only by being able to inspect all parts of the code, including the development framework.
* [.NET is open source](https://dotnet.microsoft.com/en-us/platform/open-source)

Our target is very linux and unix oriented, and the partnership between Microsoft and Canonical ensure the highest standard of security and reliability.

* [Microsoft and Canonical: partnering for security](https://ubuntu.com/blog/install-dotnet-on-ubuntu)

* [Red Hat works with Microsoft to ensure new major versions and service releases are available in tandem with Microsoft releases](https://developers.redhat.com/products/dotnet/overview)

Friendly projects for which we underline the importance of maintaining computer privacy:

* [DuckDuckGo (DDG)](https://duckduckgo.com/) is an internet search engine that emphasizes protecting searchers' privacy and avoiding the filter bubble of personalized search results.

* [The tor project](https://www.torproject.org/): is a Seattle-based 501 nonprofit organization founded by computer scientists Roger Dingledine, Nick Mathewson, and five others. The Tor Project is primarily responsible for maintaining software for the Tor anonymity network. 

* [Kali linux](https://www.kali.org/): is a Debian-derived Linux distribution designed for digital forensics and penetration testing. It is maintained and funded by Offensive Security.

* [LineageOS](https://lineageos.org/): is an Android-based operating system for smartphones, tablet computers, and set-top boxes, with mostly free and open-source software.
