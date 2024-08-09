import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from "@/components/ui/alert-dialog";

const TermsOfService = () => {
  return (
        // TODO: Change Content

    <div className="min-h-screen bg-gray-900 text-white">
      <div className="max-w-md mx-auto p-4 bg-gray-900 text-white rounded-lg shadow-md">
        <h2 className="text-center text-2xl font-bold mb-6">Terms of Service</h2>
        <div className="text-center mb-6">
          <p>
            Welcome to Prestige! Please read these Terms of Service ("Terms", "Terms of Service") carefully before using the Prestige website and mobile application (the "Service") operated by Prestige ("us", "we", or "our").
          </p>
          <h3 className="text-xl font-bold mt-4">1. Acceptance of Terms</h3>
          <p className="mt-2 text-left">
            By accessing or using our Service, you agree to be bound by these Terms. If you disagree with any part of the terms, then you do not have permission to access the Service.
          </p>
          <h3 className="text-xl font-bold mt-4">2. Accounts</h3>
          <p className="mt-2 text-left">
            When you create an account with us, you must provide us with information that is accurate, complete, and current at all times. Failure to do so constitutes a breach of the Terms, which may result in immediate termination of your account on our Service.
          </p>
          <h3 className="text-xl font-bold mt-4">3. Intellectual Property</h3>
          <p className="mt-2 text-left">
            The Service and its original content, features, and functionality are and will remain the exclusive property of Prestige and its licensors.
          </p>
          <h3 className="text-xl font-bold mt-4">4. Termination</h3>
          <p className="mt-2 text-left">
            We may terminate or suspend your account immediately, without prior notice or liability, for any reason whatsoever, including without limitation if you breach the Terms.
          </p>
          <h3 className="text-xl font-bold mt-4">5. Governing Law</h3>
          <p className="mt-2 text-left">
            These Terms shall be governed and construed in accordance with the laws of our jurisdiction, without regard to its conflict of law provisions.
          </p>
          <h3 className="text-xl font-bold mt-4">6. Changes to Terms</h3>
          <p className="mt-2 text-left">
            We reserve the right, at our sole discretion, to modify or replace these Terms at any time. If a revision is material we will provide at least 30 days' notice prior to any new terms taking effect. What constitutes a material change will be determined at our sole discretion.
          </p>
          <p className="mt-4">
            By continuing to access or use our Service after those revisions become effective, you agree to be bound by the revised terms. If you do not agree to the new terms, please stop using the Service.
          </p>
        </div>
        <AlertDialog>
        <AlertDialogTrigger className="mt-6 bg-green-600 text-white py-2 px-4 rounded hover:bg-green-700 cursor-pointer">
        Contact Us
          </AlertDialogTrigger>
          <AlertDialogContent>
            <AlertDialogHeader>
              <AlertDialogTitle>Contact Us</AlertDialogTitle>
              <AlertDialogDescription>
                If you have any questions about these Terms, please contact our support team. We are here to help!
              </AlertDialogDescription>
            </AlertDialogHeader>
            <AlertDialogFooter>
              <AlertDialogCancel>Close</AlertDialogCancel>
              <AlertDialogAction>Send Message</AlertDialogAction>
            </AlertDialogFooter>
          </AlertDialogContent>
        </AlertDialog>
      </div>
    </div>
  );
};

export default TermsOfService;
