import http from "k6/http";
import { check } from "k6";
import { htmlReport } from "https://raw.githubusercontent.com/benc-uk/k6-reporter/main/dist/bundle.js";
import { randomString } from "https://jslib.k6.io/k6-utils/1.2.0/index.js";

export let options = {
  insecureSkipTLSVerify: true,
  noConnectionReuse: false,
  stages: [
    { duration: "1m", target: 100 },
    { duration: "2m", target: 100 },
    { duration: "1m", target: 0 },
  ],
};

export default function () {
  const token =
    "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCIsImtpZCI6Ik56RTVOVFJHUVRnNVJVRkNSVFJEUkRnelEwUXdORE5GUkRZeU4wWkZNVEJGUmpCRFFUVkJRUSJ9.eyJodHRwczovL2FwaS5iZWtrLm5vL2NsYWltcy9wZXJtaXNzaW9uIjpbInJlYWQ6c3RhZmZpbmciLCJ3cml0ZTpzdGFmZmluZyIsIndyaXRlOnRlbnRhdGl2U3RhZmZpbmciLCJyZWFkOnRlbnRhdGl2U3RhZmZpbmciLCJyZWFkOmVtcGxveWVlcyIsInJlYWQ6YmVrayIsIndyaXRlOmVtcGxveWVlcyIsIndyaXRlOnRpbWVjb2RlcyIsIndyaXRlOmFjY291bnRpbmciLCJhZG1pbjppbnZvaWNlLXN2YyIsImFkbWluOlNhbGVzQW5kUHJvamVjdHMtc3ZjIiwicmVhZDpvcHBvcnR1bml0eSIsIndyaXRlOm9wcG9ydHVuaXR5Iiwid3JpdGU6cHJvamVjdCIsInJlYWRXcml0ZTpwcm9nbm9zaXMiLCJyZWFkV3JpdGU6c3ViY29udHJhY3RvciIsImFkbWluOnRpbWVrZWVwZXItc3ZjIiwicmVhZDplZ2VubWVsZGluZ2VyIiwicmVhZDp0aW1lY29kZXMiLCJyZWFkV3JpdGU6dGltZXNoZWV0cyIsInJlYWQ6aW52b2ljZXMiLCJyZWFkOmN1c3RvbWVyIiwicmVhZDpwcm9qZWN0IiwicmVhZFdyaXRlOmNhbGVuZGFyIiwiYmF0Y2hVcGRhdGU6Y2FsZW5kYXIiLCJyZWFkV3JpdGU6cGFya2luZyIsInJlYWRXcml0ZTpldmVudHMiLCJ3cml0ZTpwcmFjdGljZUdyb3VwcyIsInJlYWQ6Y3YiLCJkZWxldGU6YXV0aDB1c2VyIiwiYWRtaW46Y2FiaW4iLCJyZWFkOmZvcnNpZGUiLCJyZWFkOmNhYmluIiwiYWRtaW46YXV0aCIsImFkbWluOmVtcGxveWVlLXN2YyIsImFkbWluOmFycmFuZ2VtZW50IiwicmVhZDphcnJhbmdlbWVudCIsImFkbWluOnBhcmtpbmciLCJhZG1pbjpzcGlyaXRmb25kIiwicmVhZDphY2NvdW50aW5nIiwiYWRtaW46Y2FsZW5kYXIiLCJyZWFkOlN1YkNvbnRyYWN0b3JSZXBvcnQiLCJhZG1pbjpiZWtrYm9rIiwiYWRtaW46YXRsYXMiLCJhZG1pbjpzdGFmZmluZyJdLCJodHRwczovL2FwaS5iZWtrLm5vL2NsYWltcy9lbXBsb3llZUlkIjoxNDM3LCJuYW1lIjoiQmrDuHJuLUl2YXIgU3Ryw7htIiwiaXNzIjoiaHR0cHM6Ly9iZWtrLWRldi5ldS5hdXRoMC5jb20vIiwic3ViIjoid2FhZHxMT0FIbFBSbEJEd2JMdnhSVHdjdVNOY3FfZFkwZlBpNnJaWk1VREJTYTB3IiwiYXVkIjoiUUhReTc1Uzd0bW5oRGRCR1lTbnN6emxoTVB1bDBmQUUiLCJpYXQiOjE2OTMyODk5ODgsImV4cCI6MTY5MzMyNTk4OH0.FeEhWeBLeyimOiGVE3qCWCFXwQH2EQprKlV-ePPPNMqMyhTk2K0_4NdPXI0_WRjoo_J_0c_s37Rf9zbLUrMVqZs5e9AorO39NSyQqrYjR6uFVqooOrWYw7G-SdIsOaZ_lxAToeNOgcB5pHSwLoyFF04aJi2K4-mpomQxkZuNESYEvkr0QbGLXebL-51hqQ_vr-lKbcbugsqHcK42ta-pemBkC0aNjfSuQxtPbLvn3iKMXB6vrdXCoYpEgNrtqiLG8EdJHPhWFdqe70QIXi6e9qu3FuDYEc_G6xaaf0FPje2Kv1wtInU95gtD6UeP9m_lINUdvdCSiKpH2-HzkqMHgQ";
  const eventId = "3e2e0d14-5656-4e56-88ec-fa324e8c397a";
  let email = `${randomString(10)}@foo`;
  const params = {
    headers: {
      Authorization: `bearer ${token}`,
      "Content-Type": "application/json",
    },
  };
  const payload = {
    name: randomString(10),
    department: "Teknologi",
    email: {
      email,
    },
    // participantAnswers: ["1", "2", "3", "4", "5"],
      participantAnswers: [
      {
        questionId: "1",
        eventId,
        email,
        question: "Spørsmål 1",
        answer: "1",
      },
      {
        questionId: "2",
        eventId,
        email,
        question: "Spørsmål 2",
        answer: "2",
      },
      {
        questionId: "3",
        eventId,
        email,
        question: "Spørsmål 3",
        answer: "3",
      },
      {
        questionId: "4",
        eventId,
        email,
        question: "Spørsmål 4",
        answer: "4",
      },
      {
        questionId: "5",
        eventId,
        email,
        question: "Spørsmål 4",
        answer: "4",
      },
    ],
    cancelUrlTemplate: "http://localhost:3000/events/{eventId}/cancel/{email}?cancellationToken=%7BcancellationToken%7D",
    viewUrlTemplate: "http://localhost:3000/events/{eventId}"
  };
  const url = `http://localhost:5000/api/events/${eventId}/participants/${email}`;
  let response = http.post(url, JSON.stringify(payload), params);

  check(response, {
    "has 200 status": (r) => r.status === 200,
  });
}

export function handleSummary(data) {
  return {
    "Spike-RegisterParticpants.html": htmlReport(data),
  };
}
