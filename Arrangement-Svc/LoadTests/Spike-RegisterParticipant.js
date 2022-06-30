import http from "k6/http";
import { htmlReport } from "https://raw.githubusercontent.com/benc-uk/k6-reporter/main/dist/bundle.js";
import {
  randomString,
} from 'https://jslib.k6.io/k6-utils/1.2.0/index.js';

export let options = {
  insecureSkipTLSVerify: true,
  noConnectionReuse: false,
  stages: [
    { duration: '1m', target: 100 },
    { duration: '2m', target: 100},
    { duration: '1m', target: 0 },
  ]
}

export default function() {
  const token = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCIsImtpZCI6Ik56RTVOVFJHUVRnNVJVRkNSVFJEUkRnelEwUXdORE5GUkRZeU4wWkZNVEJGUmpCRFFUVkJRUSJ9.eyJodHRwczovL2FwaS5iZWtrLm5vL2NsYWltcy9wZXJtaXNzaW9uIjpbInJlYWQ6c3RhZmZpbmciLCJ3cml0ZTpzdGFmZmluZyIsIndyaXRlOnRlbnRhdGl2U3RhZmZpbmciLCJyZWFkOnRlbnRhdGl2U3RhZmZpbmciLCJyZWFkOmVtcGxveWVlcyIsInJlYWQ6YmVrayIsIndyaXRlOmVtcGxveWVlcyIsIndyaXRlOnRpbWVjb2RlcyIsIndyaXRlOmFjY291bnRpbmciLCJhZG1pbjppbnZvaWNlLXN2YyIsImFkbWluOlNhbGVzQW5kUHJvamVjdHMtc3ZjIiwicmVhZDpvcHBvcnR1bml0eSIsIndyaXRlOm9wcG9ydHVuaXR5Iiwid3JpdGU6cHJvamVjdCIsInJlYWRXcml0ZTpwcm9nbm9zaXMiLCJyZWFkV3JpdGU6c3ViY29udHJhY3RvciIsImFkbWluOnRpbWVrZWVwZXItc3ZjIiwicmVhZDplZ2VubWVsZGluZ2VyIiwicmVhZDp0aW1lY29kZXMiLCJyZWFkV3JpdGU6dGltZXNoZWV0cyIsInJlYWQ6aW52b2ljZXMiLCJyZWFkOmN1c3RvbWVyIiwicmVhZDpwcm9qZWN0IiwicmVhZFdyaXRlOmNhbGVuZGFyIiwiYmF0Y2hVcGRhdGU6Y2FsZW5kYXIiLCJyZWFkV3JpdGU6cGFya2luZyIsInJlYWRXcml0ZTpldmVudHMiLCJ3cml0ZTpwcmFjdGljZUdyb3VwcyIsInJlYWQ6Y3YiLCJkZWxldGU6YXV0aDB1c2VyIiwiYWRtaW46Y2FiaW4iLCJyZWFkOmZvcnNpZGUiLCJyZWFkOmNhYmluIiwiYWRtaW46YXV0aCIsImFkbWluOmVtcGxveWVlLXN2YyIsImFkbWluOmFycmFuZ2VtZW50IiwicmVhZDphcnJhbmdlbWVudCIsImFkbWluOnBhcmtpbmciLCJhZG1pbjpzcGlyaXRmb25kIiwicmVhZDphY2NvdW50aW5nIiwiYWRtaW46Y2FsZW5kYXIiLCJyZWFkOlN1YkNvbnRyYWN0b3JSZXBvcnQiLCJhZG1pbjpiZWtrYm9rIiwiYWRtaW46YXRsYXMiLCJhZG1pbjpzdGFmZmluZyJdLCJodHRwczovL2FwaS5iZWtrLm5vL2NsYWltcy9lbXBsb3llZUlkIjoxNDM3LCJuYW1lIjoiQmrDuHJuLUl2YXIgU3Ryw7htIiwiZW1haWwiOiJiam9ybi5pdmFyLnN0cm9tQGJla2subm8iLCJlbWFpbF92ZXJpZmllZCI6dHJ1ZSwiaXNzIjoiaHR0cHM6Ly9iZWtrLWRldi5ldS5hdXRoMC5jb20vIiwic3ViIjoid2FhZHxMT0FIbFBSbEJEd2JMdnhSVHdjdVNOY3FfZFkwZlBpNnJaWk1VREJTYTB3IiwiYXVkIjoiUUhReTc1Uzd0bW5oRGRCR1lTbnN6emxoTVB1bDBmQUUiLCJpYXQiOjE2NTY1NzA2MDIsImV4cCI6MTY1NjYwNjYwMn0.t3IT2jMIHn6HFuIX9V3VJ0jFyNuC4HNt9PKwb6LmSjxmZgo7YiTnnoZGMhvlkIlAjVwaTc9hKdEt0_ZgrohCpbwxB6Yp9Va7Tty_YBMoZfG4DTJtt53yrLnpjKhAjTUpMPsuQEDjAPmKQCffTiqm6pihSM7OukflfJccuoDR36HbuQVadTwdceYoHmrWWpZz_AjGKmJufUBxYmxP8mY8OoZpm5gbSpWeACJiWlOeuN7145qZQ80Iuj0lVo8ve0Jirx3L57J-zYmZ_uELFij11FobPMrtUeOx8u4B0YKcBzXBzAcsZ8S5rZjqNRQ7bGUKQlNOM3QP1WUjCcSJV3x2gw"
  const eventId = "a737ff9b-8163-4be9-8f97-2ade33ecc19c"
  let email = `${randomString(10)}@foo`
  const params = {
    headers: {
      Authorization: `bearer ${token}`,
      "Content-Type": "application/json"
    }
  }
  const payload = {
    name: randomString(10),
    email: {
      email
    },
    participantAnswers: [],
    cancelUrlTemplate: "http://localhost:3000/events/{eventId}/cancel/{email}?cancellationToken=%7BcancellationToken%7D",
  };
  const url = `http://localhost:5000/events/${eventId}/participants/${email}`
  let response = http.post(url, JSON.stringify(payload), params);
};

export function handleSummary(data) {
  return {
    "Spike-RegisterParticpants.html": htmlReport(data),
  };
}
