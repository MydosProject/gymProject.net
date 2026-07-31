pipeline {
    agent any
    
    environment {
        DOCKER_CRED_ID = 'dockerhub-cred'
        IMAGE_NAME = 'mehmetevg/gymproject:latest'
        K3S_CRED_ID = 'k3s-kubeconfig'
    }
    
    stages {
        stage('Kodu Indir') {
            steps {
                checkout scm
            }
        }
        
        stage('Imaj Yap') {
            steps {
                sh 'docker build -t $IMAGE_NAME .'
            }
        }
        
        stage('Hub a Yolla') {
            steps {
                withCredentials([usernamePassword(credentialsId: env.DOCKER_CRED_ID, passwordVariable: 'PASS', usernameVariable: 'USER')]) {
                    sh 'echo $PASS | docker login -u $USER --password-stdin'
                    sh 'docker push $IMAGE_NAME'
                }
            }
        }
        
        stage('Canliya Al') {
            steps {
                withCredentials([file(credentialsId: env.K3S_CRED_ID, variable: 'KUBECONFIG')]) {
                    sh 'kubectl apply -f k8s.yaml'
                    sh 'kubectl rollout restart deployment gymproject-deploy'
                }
            }
        }
    }
}
