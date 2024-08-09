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

const PrivacyPolicy = () => {
  return (
    // TODO: Change Content
    <div className="min-h-screen bg-gray-900 text-white">
      <div className="max-w-md mx-auto p-4 bg-gray-900 text-white rounded-lg shadow-md">
        <h2 className="text-center text-2xl font-bold mb-6">Privacy Policy</h2>
       
        <div className="text-center mb-6">
          <p>
            At Prestige, we value your privacy and are committed to protecting your personal information. This Privacy Policy outlines the types of information we gather, how we use it, and the measures we take to safeguard your data.
          </p>
          <h3 className="text-xl font-bold mt-4">Information We Gather</h3>
          <ul className="list-disc list-inside mt-2 text-left">
            <li>Personal Identification Information (name, email address, phone number, etc.)</li>
            <li>Usage Data (listening habits, preferences, interaction with our services)</li>
            <li>Technical Data (IP address, browser type, device information)</li>
            <li>Cookies and similar tracking technologies</li>
          </ul>
          <h3 className="text-xl font-bold mt-4">How We Use Your Information</h3>
          <ul className="list-disc list-inside mt-2 text-left">
            <li>To provide and maintain our services</li>
            <li>To personalize your experience</li>
            <li>To improve our services and develop new features</li>
            <li>To communicate with you, including sending updates and promotional materials</li>
            <li>To ensure the security and integrity of our services</li>
            <li>To comply with legal obligations</li>
          </ul>
          <p className="mt-4">
            We implement a variety of security measures to protect your personal information. Despite our efforts, no security measures are perfect or impenetrable, and we cannot guarantee the security of your data.
          </p>
          <p className="mt-4">
            For more detailed information, please refer to the full version of our Privacy Policy or contact our support team.
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
        If you have any questions or concerns about our Privacy Policy, please reach out to our support team. We are here to help!
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

export default PrivacyPolicy;
