# Haptic Vision

![haptx](https://user-images.githubusercontent.com/39020723/218214123-f66f583e-3f93-461e-9d06-ebd59a3d3cc9.jpg)
Demo and Pitch Vide0 at the bottom


# Inspiration
Visually impaired individuals face significant barriers when trying to navigate and interact with the physical world, which can lead to a lack of independence and reduced quality of life. According to WHO, there are approximately 253 million people worldwide who are visually impaired. Many common everyday tasks, such as grocery shopping, traveling, and accessing public transportation, can be difficult or even impossible for visually impaired individuals to accomplish without assistance. Currently, visually impaired individuals rely on tools such as support canes and guide dogs, but these tools have limitations. 

### Goal
The goal is to design a solution that addresses the specific needs of visually impaired individuals, providing them with the tools and resources they need to navigate and interact with the physical world independently, safely, and with confidence.

---

# What it does
Haptic Vision is an inclusive technology that allows individuals to navigate their physical surroundings with greater ease and understanding. By utilizing sound and haptic feedback, Haptic Vision is an extension of a traditional seeing cane, enabling users to sense the presence of nearby objects and furniture. The user wears a VR headset and Haptx gloves, and as their hands approach objects like furniture, they will feel a vibration sensation as if they are physically touching the object. The closer they get to the object, the stronger and more detailed the sensation becomes, providing a clear understanding of its location and size. Additionally, an audio alarm is triggered to further aid in navigation. 
Our product is designed for individuals of all abilities. For those with visual impairments, Haptic Vision is an essential tool that can assist in understanding and navigating their environment. For those without visual impairments, Haptic Vision offers a unique, empathetic experience, allowing them to understand better the challenges faced by those with visual impairments.

We envision Haptic Vision as a pioneering solution for the future of navigation for individuals with visual impairments. By leveraging cutting-edge technologies such as XR, we aim to enhance the senses and empower those with visual impairments to navigate confidently and independently, even in obstacles. Haptic Vision is a glimpse into the next 5-10 years, where innovative technologies will revolutionize how people with visual impairments interact with the world around them.

---

# How we built it
### We defined the Problem
We spent time on a glass window writing (with erasable marker lol) different problems we identified with different types of visually impaired people. We narrowed down the project to focus on a specific problem, which was the fact that Visually Impaired users have a difficult time navigating their environment confidently.

### We collaborated to come up with Solutions 
On the same board, we collaborated on different ideas to approach a solution. Some of the ideas explored improving vision with enhancements of visuals, audio systems, and haptic feedback. During this phase, we also conducted user research by communicating with Visually impaired people. To learn about what painpoints they experience, how they "see", what tools they use, and what they want. With this, we were also able to create a persona.

### We decided on the MVP Solution 
We narrowed down the problem to a specific solution involving Haptic Feedback. And we defined the MVP of the product to ensure we don't sidetrack ourselves in this concise time frame. 

Let's create a way to detect objects and use haptics to let users feel how far that object is.

### We developed a prototype
The project was developed using Unity and the HTC Vive Pro Eye with Lighthouses, as HapTx hand tracking is compatible with any VR HMD that uses Lighthouses and Windows. The following list summarizes the key features and technologies used:

**Technologies**
HapTx
HTC Vive pro eye 
Unity 19.4.31f

**SDKs**
HapTx 2.0.0 beta 8
SRworks 

**Physical Environment and Mixed Reality**
Using SR works, we created a 3D model of the environment. However, we faced challenges with implementing hand tracking.

**Hand Tracking**
We chose to use the HapTx SDK, compatible with the same version of Unity used for SR works.

**Object Detection**
SRworks was utilized for object detection, and the AI model provided by SR works can identify common objects such as chairs and tables.

**Distance Approximation**
Ray casting, built into Unity, determined the distance from the hand to an object.

**Haptics**
Based on the distance, we set the frequency and amplitude of the gloves, with a maximum frequency of 30 Hz and a minimum of 15 Hz. The amplitude remains untested at this time.

**Testing**
The project was tested on Windows 11, which supports VR development.

**Staging**
The project was hosted on Github, with the "works" branch used for development and testing, and the main code was published under the "main" branch.

### We tested on actual users
We got a visually impaired user to test out the project in the end. And the feedback was amazing. 

---

# Challenges we ran into 
For every part of development, we ran into challenges. Here’s a summary of the different parts and how we approached them 

### Physical Environment/ Mixed Reality
**Problem**
SRworks only worked with a version of unity that wasn’t supported (unity v17)
This took a lot of time on our end.

**Solution**
We were able to solve this by talking with the mentors from VIVE. However, the time frame was not expected. We ended up picking v19.1 as support for it was discovered.

### Hand Tracking
**Problem**
Hand tracking was challenging to pull off with SRworks activated. 

**Solution**
Because of time constrictions, we opted to make them face one direction. They worked with the Haptix gloves. 
We also decided to put it a certain distance away from the actual hands so that they could interact with the mesh.


### Distance Approximation
**Problem**
With Ray Casting(Built into unity), we could get the distance from the ray Caster(hand) to the object. This approach was buggy as the ray cast would always detect an object, especially in an enclosed house.

**Solution**
We cast the hands of a couple of units in front of where they are supposed to be. That way interaction with the mesh was more smooth.

### Haptics
**Problem**
When it came to testing, only two devices were present. Building and testing was the only way. 
Testing for frequency and amplitude was difficult to test

**Solution**
Increasing and decreasing frequency with amplitude remaining constant got the work done

---

# Accomplishments that we're proud of

Getting SRworks to run
Getting HaptX to work on the 2019 version of Unity when the only available version was 2021. Because SRworks worked only in 2019.
Getting SRworks to communicate with HaptX

---

# What we learned

Different types of visual impairments. IE Color blindness, Donuts, etc
How the Visually Impaired can “see” currently
How companies are currently trying to solve issues. Sunu Band, Canes, Radar Mapping

---

# What's next for Haptic Vision
Enabling Audio Feedback
Active Scanning with Headset with on/off feature
Smaller Headset. Make it seamless and look like normal glasses.
Improve vision through XR glasses recalibration
Waypoint Guidance?


# Pitch Video (Click on the image)
[![Watch the video](https://d112y698adiu2z.cloudfront.net/photos/production/software_photos/002/346/697/datas/original.jpg)](https://youtu.be/4b5lR5_ubpw)

# Demo Videos
[Demo 1](https://photos.app.goo.gl/2GCe3nBR772Doe9V9)
[Demo 2](https://photos.app.goo.gl/ZfDBynpsBgJS3u7w7)

